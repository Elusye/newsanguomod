using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Monsters;

/// <summary>
/// 青州兵 —— 「曹氏兵法」独立召唤的消耗型召唤物（与奥斯提区分开的新召唤物）。
///
/// 定位：1 生命值、不攻击、只替主人挡刀的“兵”。
///  · 每次召唤都是独立个体（不走原版 <c>OstyCmd.Summon</c> 的“一只、血量累加”合并逻辑）；
///  · 承伤由 <c>DieForYouPower</c> 提供（挂在这只兵身上，只替 PetOwner 接“强化攻击”），
///    承伤优先级：青州兵（多只之间按召唤顺序依次穿透）→ 玩家格挡 → 奥斯提 → 玩家，
///    见 Scripts/Patches/CaoArtOfWarSoldierDamageCascadePatch.cs；
///  · 阵亡即离场（见 Scripts/Patches/CaoArtOfWarSoldierRemovalPatch.cs），不留尸体等复活；
///  · 回合开始时被「曹氏兵法」清场。
///
/// 形象：复用原版奥斯提的 Spine 场景（见 <see cref="AssetProfile"/> 与 <see cref="GenerateAnimator"/>）。
/// 2026-10-09 曾按用户需求 m00823 短暂换成青州兵自己的贴图，同日用户决定换回奥斯提，并把显示名改叫
/// 「青州兵（迫真）」（见 <see cref="Title"/>，文案在 monsters.json），算是对“借用奥斯提形象”的自嘲。
/// 音效：死亡不发声（用户 2026-10-09 拍板「死亡直接消失」，见 <see cref="HasDeathSfx"/> 的注释）；
/// 受击是原版通用的护甲撞击音（与形象无关）。
/// </summary>
[RegisterMonster]
public class QingzhouSoldier : ModMonsterTemplate
{
    // 招式状态名（bestiary 的本地化键会用到；宠物不攻击，只留一个原地待命的空招式）
    private const string NothingMoveState = "NOTHING_MOVE";

    public override int MinInitialHp => 1;

    public override int MaxInitialHp => 1;

    // 战斗形象：复用原版奥斯提的 Spine 场景（2026-10-09 用户拍板换回奥斯提，显示名改成「青州兵（迫真）」）
    public override MonsterAssetProfile AssetProfile => new("res://scenes/creature_visuals/osty.tscn");

    // 死亡音效：形象换回奥斯提的 Spine 场景后，引擎的死亡流程（含死亡音）是可达的
    // —— NCreature.StartDeathAnim 在 `if (_spineAnimator != null)` 里才调 SfxCmd.PlayDeath
    //（.decompile/sts2full/sts2.decompiled.cs:477104-477134），而 _spineAnimator 只在
    //`if (HasSpineAnimation)` 里赋值（同文件 :476680-476698）。用户 2026-10-09 拍板「死亡直接消失」，
    // 所以这里保留奥斯提的音效路径但用 HasDeathSfx 把它静音，死亡表现＝引擎的淡出 + 出队
    //（配合 Scripts/Patches/CaoArtOfWarSoldierRemovalPatch.cs，不留尸体）。
    public override string DeathSfx => "event:/sfx/characters/osty/osty_die";

    // 引擎只在自带 Spine 分支里查这个属性；写 false 即死亡不出声。
    //（想恢复奥斯提那声死亡叫就把这里改成 true，字符串已经在上面备好。）
    public override bool HasDeathSfx => false;

    // 同原版奥斯提：活着才显示血条
    public override bool IsHealthBarVisible => base.Creature.IsAlive;

    // 名称走 monsters 表。默认实现是 L10NMonsterLookup(Id.Entry + ".name")，
    // 这里显式写死同一个键，避免以后改类名/前缀时名字突然变空。
    // 当前文案（用户 2026-10-09 指定）：zhs「青州兵（迫真）」/ eng「Qingzhou Soldier (Allegedly)」。
    public override LocString Title => MonsterModel.L10NMonsterLookup("NEWSANGUO_MONSTER_QINGZHOU_SOLDIER.name");

    // 与奥斯提同构：只有一个“什么都不做”的招式并自循环（宠物不行动，但需要一条合法的状态机）
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState idle = new(NothingMoveState, (IReadOnlyList<Creature> _) => Task.CompletedTask);
        idle.FollowUpState = idle;
        return new MonsterMoveStateMachine(new List<MonsterState> { idle }, idle);
    }

    // 与奥斯提同一套动画名（因为复用它的 Spine 场景）：idle_loop / cast / attack / hurt / die / dead_loop / revive。
    // 引擎只在 `if (HasSpineAnimation)` 里调 GenerateAnimator（.decompile/sts2full/sts2.decompiled.cs:476680-476698）
    // ⇒ 当前用奥斯提 Spine 场景，这里的动画名是生效的；若哪天再换成静态贴图，本方法会变成死代码（不报错）。
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("idle_loop", isLooping: true);
        AnimState cast = new("cast");
        AnimState attack = new("attack");
        AnimState attackPoke = new("attack_poke");
        AnimState hurt = new("hurt");
        AnimState die = new("die");
        AnimState deadLoop = new("dead_loop", isLooping: true);
        AnimState revive = new("revive");

        idle.AddBranch("Hit", hurt);
        cast.NextState = idle;
        cast.AddBranch("Hit", hurt);
        attack.NextState = idle;
        attack.AddBranch("Hit", hurt);
        attackPoke.NextState = idle;
        attackPoke.AddBranch("Hit", hurt);
        hurt.NextState = idle;
        hurt.AddBranch("Hit", hurt);
        die.NextState = deadLoop;
        revive.NextState = idle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("attack_poke", attackPoke);
        animator.AddAnyState("Revive", revive);
        return animator;
    }
}
