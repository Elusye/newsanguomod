using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Monsters;

/// <summary>
/// 盛碗虫（巨石）BowlbugBoulder —— 原版 BowlbugRock 的强化版（照其 BowlbugRock.cs 改写）：
///  · 意图与招式循环与原版完全一致：头槌（SingleAttackIntent）→ 失衡则眩晕（StunIntent）→ 循环；
///  · 血量 = BowlbugRock × 3（45~48 → 135~144；攀升 ToughEnemies 下 46~49 → 138~147）；
///  · 体型 = 原版的两倍（复用同一套 Spine 场景，在 SetupSkins 里随节点创建就放大，
///    并在 AfterAddedToRoom 里再确认一次；见这两处的注释）；
///  · 头槌伤害固定 25（原版是 15/16 随攀升变化，这里不随攀升提高）；
///  · 失衡用自建能力 BowlbugBoulderImbalancedPower（逻辑同原版，但原版把持有者硬编码成 BowlbugRock）；
///  · 只在事件「实践主义者」里登场，且仅当本幕是 Hive（见 Pragmatist.cs 的 CanonicalEncounter）。
/// 形象沿用原版 bowlbug_rock 的 Spine 场景与“rock”皮肤（巨石＝同一只虫的岩皮形态），
/// 以后要换成自己的图，只需改 AssetProfile 的 VisualsScenePath（非 Spine 图片状态也可以走 RitsuLib 的那条路）。
/// </summary>
[RegisterMonster]
public class BowlbugBoulder : ModMonsterTemplate
{
    // 招式状态名（bestiary 的本地化键会用到；名称照搬原版 BowlbugRock）
    private const string HeadbuttMoveState = "HEADBUTT_MOVE";
    private const string DizzyMoveState = "DIZZY_MOVE";

    // 音效沿用 bowlbug_rock（默认按 id 拼路径会指向不存在的 bowlbug_boulder_*）
    private const string StunSfx = "event:/sfx/enemy/enemy_attacks/workbug_rock/workbug_rock_stun";

    // 失衡标记：与原版一样，由“攻击被完全格挡”置位（见 BowlbugBoulderImbalancedPower）
    private bool _isOffBalance;

    public bool IsOffBalance
    {
        get => _isOffBalance;
        set
        {
            AssertMutable();
            _isOffBalance = value;
        }
    }

    // 血量 = 原版 BowlbugRock 的 3 倍：直接读它的当前数值再乘，因此高攀升时自动跟着涨
    // （BowlbugRock：非攀升 45~48、ToughEnemies 46~49；×3 → 135~144 / 138~147）
    public override int MinInitialHp => (int)(ModelDb.Monster<BowlbugRock>().MinInitialHp * 3m);

    public override int MaxInitialHp => (int)(ModelDb.Monster<BowlbugRock>().MaxInitialHp * 3m);

    // 头槌伤害：固定 25（“每回合打 25”）
    public static int HeadbuttDamage => 25;

    public override string DeathSfx => "event:/sfx/enemy/enemy_attacks/workbug_rock/workbug_rock_die";

    protected override string AttackSfx => "event:/sfx/enemy/enemy_attacks/workbug_rock/workbug_rock_attack";

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Insect;

    public override MonsterAssetProfile AssetProfile => new("res://scenes/creature_visuals/bowlbug_rock.tscn");

    // 沿用“岩皮”皮肤（与原版 BowlbugRock 一致），并在这里就把体型放大到 2 倍。
    //
    // 为什么体型必须在这里设：这是**节点创建时**唯一会回调到模型侧的钩子
    // （NCreature._Ready → Visuals.SetUpSkin(Entity.Monster) → MonsterModel.SetupSkins，
    //  见 NCreature.cs:182 / NCreatureVisuals.cs:131-141），事件布局的展示用敌人同样走这条路。
    // 而 MonsterModel.AfterAddedToRoom 只在**真正开打**时才被调用
    // （CombatManager.AfterCreatureAdded → Creature.AfterAddedToRoom，CombatManager.cs:860-862）：
    // 事件「实践主义者」用 Combat 布局，事件页面上先出现的敌人是 EventModel.GenerateInternalCombatState
    // 直接 CombatState.AddCreature 造出来的（EventModel.cs:391-402），不经过 CombatManager，
    // 所以那些节点在“刚刷出来”时根本没跑过 AfterAddedToRoom —— 这正是
    // “事件里刚出现时是原版大小、点「战斗」后才瞬间变大”的原因。
    //
    // 取节点的方式：SetupSkins 只拿得到 SpineSprite（= %Visuals 节点，NCreatureVisuals.cs:84-93），
    // 它在 NCreatureVisuals 子树里，往上找到那层即可；找不到就静默跳过（AfterAddedToRoom 仍会兜底）。
    public override void SetupSkins(MegaSprite spine, MegaSkeleton skeleton)
    {
        skeleton.SetSkin(skeleton.GetData().FindSkin("rock"));
        skeleton.SetSlotsToSetupPose();

        if (spine.BoundObject is not Node spineNode)
        {
            return;
        }

        for (Node? node = spineNode; node is not null; node = node.GetParent())
        {
            if (node is NCreatureVisuals visuals)
            {
                visuals.SetScaleAndHue(BoulderScale, 0f);
                return;
            }
        }
    }

    // 兜底入口：把场上某个已经存在的怪物节点放大（节点无效/还没 _Ready 时静默跳过）。
    // 目前由事件「实践主义者」在布置/显形敌人时顺带调用，避免依赖模型回调的时机。
    public static void ApplyScaleToNode(NCreature? node)
    {
        if (node is null || !GodotObject.IsInstanceValid(node) || !node.IsNodeReady())
        {
            return;
        }

        node.SetScaleAndHue(BoulderScale, 0f);
    }

    // 入场时挂上失衡标记能力（时机与数值同原版：AfterAddedToRoom + 1 层），并把体型放大到 2 倍
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<BowlbugBoulderImbalancedPower>(
            new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        TaskHelper.RunSafely(ApplyBoulderScale());
    }

    // —— 体型：原版 BowlbugRock 的两倍大 ——
    // 用 NCreature.SetScaleAndHue(2f, 0f) 而不是 ScaleTo(2f)，原因是“意图图标的位置”：
    //   · NCreature.UpdateBounds() 才会重排意图容器，且它的 Y 是乘上 Visuals.Scale.X 的
    //     （NCreature.cs:270-271：IntentContainer.Position.Y *= Visuals.Scale.X）；
    //   · ScaleTo() 只跑补间回调 DoScaleTween()（NCreature.cs:803-807），**不会**调用 UpdateBounds，
    //     所以放大后意图仍按旧高度显示，看起来“偏低”；
    //   · SetScaleAndHue() 会调 UpdateBounds（NCreature.cs:742-746），顺手把判定框/选中框一起刷新；
    //     它的第二参数是色相偏移，传 0 表示不改色（NCreatureVisuals.cs:143-148）。
    // 另外本怪单独出场：NCombatRoom 只为“同组多只怪”做体型压缩（NCombatRoom.cs:687-701 对
    // Count == 1 直接 continue），所以不会有人把我们这个 2 倍覆盖掉。
    private const float BoulderScale = 2f;

    private static readonly double[] ScaleRetryDelays = [0.0, 0.15];

    private async Task ApplyBoulderScale()
    {
        // 视觉节点可能比模型回调晚一点生成，这里刷两次确保生效（重复设置很便宜）
        foreach (double delay in ScaleRetryDelays)
        {
            await Cmd.Wait((float)delay);
            NCombatRoom.Instance?.GetCreatureNode(base.Creature)?.SetScaleAndHue(BoulderScale, 0f);
        }
    }

    // 招式状态机：与 BowlbugRock 完全同构
    // 恒常循环：HEADBUTT → 条件分支（失衡 → DIZZY，否则 → HEADBUTT）
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = [];

        MoveState headbutt = new(HeadbuttMoveState, HeadbuttMove, new SingleAttackIntent(HeadbuttDamage));
        MoveState dizzy = new(DizzyMoveState, DizzyMove, new StunIntent());

        ConditionalBranchState postHeadbutt = new("POST_HEADBUTT");
        headbutt.FollowUpState = postHeadbutt;
        dizzy.FollowUpState = headbutt;
        postHeadbutt.AddState(dizzy, () => IsOffBalance);
        postHeadbutt.AddState(headbutt, () => !IsOffBalance);

        states.Add(dizzy);
        states.Add(postHeadbutt);
        states.Add(headbutt);
        return new MonsterMoveStateMachine(states, headbutt);
    }

    // 头槌：造成 HeadbuttDamage 点伤害；若此时处于失衡，紧跟着眩晕（下一回合打 DIZZY_MOVE）
    private async Task HeadbuttMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(HeadbuttDamage).FromMonster(this).WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        if (IsOffBalance)
        {
            await Stun();
        }
    }

    private async Task Stun()
    {
        SfxCmd.Play(StunSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Stun", 0.6f);
        await CreatureCmd.Stun(base.Creature, DizzyMove);
    }

    // 眩晕回合：清掉失衡标记并播“醒来”动画
    private async Task DizzyMove(IReadOnlyList<Creature> targets)
    {
        IsOffBalance = false;
        await CreatureCmd.TriggerAnim(base.Creature, "Unstun", 0.6f);
    }

    // 动画状态机：与原版 BowlbugRock 相同（复用同一套 Spine 动画名）
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("idle_loop", isLooping: true);
        AnimState cast = new("buff");
        AnimState attack = new("headbutt");
        AnimState hurt = new("hurt");
        AnimState hurtStunned = new("hurt_stunned");
        AnimState wakeUp = new("wake_up");
        AnimState dead = new("die");
        AnimState stun = new("stun");
        AnimState stunnedLoop = new("stunned_loop", isLooping: true);

        // 非循环动画播完后回到哪个状态（逐行对齐原版 BowlbugRock.cs:120-125）：
        //   buff / hurt / headbutt / wake_up → idle_loop
        //   stun / hurt_stunned             → stunned_loop
        // 注意：这里的 NextState 必须指向 idle_loop，绝不能用 die —— 指向 die 的话，
        // 「醒来」动画放完就会直接接死亡动画（曾经写错成 die，导致眩晕醒来后播放死亡动画）。
        cast.NextState = idle;
        hurt.NextState = idle;
        attack.NextState = idle;
        stun.NextState = stunnedLoop;
        hurtStunned.NextState = stunnedLoop;
        wakeUp.NextState = idle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Hit", hurt, () => !IsOffBalance);
        animator.AddAnyState("Hit", hurtStunned, () => IsOffBalance);
        animator.AddAnyState("Stun", stun);
        animator.AddAnyState("Unstun", wakeUp);
        return animator;
    }

    // 图鉴：插一条“眩晕”招式（与原版一致）
    public override List<BestiaryMonsterMove> GenerateBestiaryMoveList(NCreatureVisuals? creatureVisuals)
    {
        List<BestiaryMonsterMove> list = base.GenerateBestiaryMoveList(creatureVisuals);
        list.Insert(1, BestiaryMonsterMove.FromStun(Stun));
        return list;
    }

    // 名称走 monsters 表。默认实现是 L10NMonsterLookup(Id.Entry + ".name")，
    // 这里显式写死同一个键，避免以后改类名/前缀时名字突然变空。
    public override LocString Title => MonsterModel.L10NMonsterLookup("NEWSANGUO_MONSTER_BOWLBUG_BOULDER.name");
}

/// <summary>
/// 事件专用遭遇：只放一只盛碗虫（巨石）。
///  · RoomType 用 Elite：事件里打赢后拿到与原版精英一致的奖励；
///  · 不设 HasScene，槽位保持 null —— 与 WildNeutralAmbushers 的 SkulkingColonyEffigyEncounter 同样的注意事项：
///    HasScene=true 会让 NCombatRoom 去找不存在的 res://scenes/encounters/&lt;id&gt;.tscn；
///    槽位非 null 而 EncounterSlots 为空时 AddCreature 会抛异常。保持默认走 PositionEnemies 自动布局。
/// </summary>
public sealed class BowlbugBoulderEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<BowlbugBoulder>()
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<BowlbugBoulder>().ToMutable(), null)
        ];
    }
}
