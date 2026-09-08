using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “忽近忽远”：在接下来的回合内，交替获得与层数相同的临时敏捷和临时力量。
/// 多次打出此卡不会合并层数，而是创建独立的实例（PowerInstanceType.Instanced），
/// 各实例独立维护交替相位：
/// · 同一回合内连续打出多张：相位相同，等效合并（如两张 → 交替获得 6 力 6 敏）；
/// · 不同回合各自打出：相位互补，每回合同时获得力量与敏捷（两张 → 常驻 3 力 3 敏）。
/// 额外附带“沙坑式”身位演出：打出回合靠近怪物一段距离，之后每个获得临时力量的回合
/// 再靠近、获得临时敏捷的回合再远离，每次移动量 = 沙坑每回合拖拽距离（远/近全程的 1/6）。
/// </summary>
[RegisterPower]
public class NearAndFarPower : ModPowerTemplate
{
    private class Data
    {
        // 本实例创建时的玩家回合号
        public int createdTurn = -1;
        // 下一个回合先获得敏捷，之后交替
        public bool nextIsDexterity = true;
        // 本实例定格的位移锚点（远点=创建时站位 X，近点=最近存活敌人身前 X）
        public bool motionValid;
        public float homeX;
        public float nearX;
        public float stepX;
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 多次打出创建独立实例，各自维持独立的交替节奏，不合并层数
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    // 层数即每次交替获得的量
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    private Data GetData() => GetInternalData<Data>();

    // 施加完成后确定本实例的交替相位，并定格“沙坑式”位移锚点：
    // · 同一回合内已打出的实例相位相同（合并效果，交替获得总层数）；
    // · 新回合打出的实例与最近一次打出的实例相位互补（各自独立，每回合都有力有敏）。
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        int currentTurn = Owner?.Player?.PlayerCombatState?.TurnNumber ?? -1;
        Data data = GetData();
        data.createdTurn = currentTurn;

        // 同回合已存在的实例：继承其相位
        NearAndFarPower? sameTurn = Owner?.Powers.OfType<NearAndFarPower>()
            .FirstOrDefault(p => p != this && p.GetData().createdTurn == currentTurn);
        if (sameTurn is not null)
        {
            data.nextIsDexterity = sameTurn.GetData().nextIsDexterity;
        }
        else
        {
            // 新回合的第一张：与最近一次打出的实例反相
            NearAndFarPower? previous = Owner?.Powers.OfType<NearAndFarPower>()
                .Where(p => p != this && p.GetData().createdTurn >= 0)
                .OrderByDescending(p => p.GetData().createdTurn)
                .FirstOrDefault();
            if (previous is not null)
            {
                data.nextIsDexterity = !previous.GetData().nextIsDexterity;
            }
        }

        // 定格本实例的位移锚点（仅当战斗内存在可瞄准的存活敌人）
        if (Owner is not null && NearAndFarMover.TryCapture(Owner, out float homeX, out float nearX, out float stepX))
        {
            data.motionValid = true;
            data.homeX = homeX;
            data.nearX = nearX;
            data.stepX = stepX;

            // 打出回合：本卡当场赋予临时力量，故立即朝怪物身前靠近一个沙坑步长
            await NearAndFarMover.MoveNearAsync(Owner, homeX, nearX, stepX);
        }
    }

    // 回合开始时：交替获得临时敏捷/力量，并按属性执行“沙坑式”靠近/远离
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player is null || player.Creature != Owner || Amount <= 0)
        {
            return;
        }

        Flash();

        // 触发音效：交替获得临时敏捷/力量
        NewsanguoSfx.Play("event:/newsanguo/sfx/near_and_far_power");

        Data data = GetInternalData<Data>();
        bool grantDexterity = data.nextIsDexterity;
        if (grantDexterity)
        {
            await PowerCmd.Apply<NearAndFarDexterityPower>(choiceContext, Owner, Amount, Owner, null, silent: false);
        }
        else
        {
            await PowerCmd.Apply<NearAndFarStrengthPower>(choiceContext, Owner, Amount, Owner, null, silent: false);
        }

        // 翻转：下次获得另一种属性
        data.nextIsDexterity = !grantDexterity;

        // 沙坑式身位：敏捷回合远离、力量回合靠近（每次移动一个沙坑步长）
        if (data.motionValid && data.stepX > 0f)
        {
            if (grantDexterity)
            {
                await NearAndFarMover.MoveFarAsync(Owner, data.homeX, data.nearX, data.stepX);
            }
            else
            {
                await NearAndFarMover.MoveNearAsync(Owner, data.homeX, data.nearX, data.stepX);
            }
        }
    }
}

/// <summary>
/// “忽近忽远”专用的“沙坑式”位移工具：参考原版 SandpitPower 的身位演出，
/// 近点 = 最靠近玩家的存活敌人“身前”（敌人 X−450，面向玩家一侧），
/// 远点 = 锚点定格时的玩家站位，每步距离 = |近点−远点| / 6
/// （对应沙坑施加 6 层时每回合拖拽的那一格），用 tween 平滑移动玩家生物节点。
/// </summary>
internal static class NearAndFarMover
{
    // 与 SandpitPower 相同的几何常量
    private const float EnemyFrontPadding = 450f; // 敌人“身前”距怪物中心的距离（沙坑的近点）
    private const int TotalStepDivisor = 6;       // 折算每步：全程的 1/6（沙坑初始 6 层时每回合格）
    private const float MoveDuration = 0.25f;     // 补间时长，与沙坑一致

    /// <summary>抓取位移锚点。成功返回 true 并给出远点/近点 X 与每步距离；战斗外、无存活敌人等场景返回 false。</summary>
    public static bool TryCapture(Creature? creature, out float homeX, out float nearX, out float stepX)
    {
        homeX = 0f;
        nearX = 0f;
        stepX = 0f;
        if (creature is null)
        {
            return false;
        }
        ICombatState? combatState = creature.CombatState;
        NCombatRoom? room = NCombatRoom.Instance;
        if (combatState is null || room is null)
        {
            return false;
        }
        NCreature? meNode = room.GetCreatureNode(creature);
        if (meNode is null)
        {
            return false;
        }
        homeX = meNode.GlobalPosition.X;

        // 选最靠近玩家的存活敌人作为“靠近目标”
        Creature? chosen = null;
        float bestDistance = float.PositiveInfinity;
        foreach (Creature enemy in combatState.Enemies)
        {
            if (enemy.IsDead)
            {
                continue;
            }
            NCreature? node = room.GetCreatureNode(enemy);
            if (node is null)
            {
                continue;
            }
            float distance = Mathf.Abs(node.GlobalPosition.X - homeX);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                chosen = enemy;
            }
        }
        if (chosen is null)
        {
            return false;
        }
        NCreature? enemyNode = room.GetCreatureNode(chosen);
        if (enemyNode is null)
        {
            return false;
        }

        // 敌人“身前”朝向玩家一侧 450px 处；敌人站在右侧则取其左侧 450px，反之取右侧
        float enemyX = enemyNode.GlobalPosition.X;
        float mouthX = enemyX >= homeX ? enemyX - EnemyFrontPadding : enemyX + EnemyFrontPadding;
        float gap = Mathf.Abs(mouthX - homeX);
        if (gap < 1f)
        {
            return false; // 已紧贴，无可移动空间
        }
        float step = gap / TotalStepDivisor;
        if (step < 1f)
        {
            return false; // 步长过小，视觉无意义
        }
        nearX = mouthX;
        stepX = step;
        return true;
    }

    /// <summary>向“近点”（怪物身前）移动一个沙坑步长。</summary>
    public static async Task MoveNearAsync(Creature? creature, float homeX, float nearX, float stepX)
    {
        if (creature is null)
        {
            return;
        }
        NCreature? node = GetNode(creature);
        if (node is null)
        {
            return;
        }
        await MoveStepAsync(node, Mathf.MoveToward(node.GlobalPosition.X, nearX, stepX));
    }

    /// <summary>向“远点”（锚点定格时的站位）移动一个沙坑步长。</summary>
    public static async Task MoveFarAsync(Creature? creature, float homeX, float nearX, float stepX)
    {
        if (creature is null)
        {
            return;
        }
        NCreature? node = GetNode(creature);
        if (node is null)
        {
            return;
        }
        await MoveStepAsync(node, Mathf.MoveToward(node.GlobalPosition.X, homeX, stepX));
    }

    private static NCreature? GetNode(Creature creature)
    {
        if (NCombatRoom.Instance is not NCombatRoom room)
        {
            return null;
        }
        // 目标敌人已全灭时不再做位移演出
        bool anyAlive = creature.CombatState?.Enemies.Any(e => !e.IsDead) == true;
        return anyAlive ? room.GetCreatureNode(creature) : null;
    }

    // 用与沙坑一致的补间把节点平滑移动到目标 X
    private static async Task MoveStepAsync(NCreature node, float targetX)
    {
        if (Mathf.Abs(targetX - node.GlobalPosition.X) < 0.5f)
        {
            return; // 位移过小，跳过
        }
        NCombatRoom? room = NCombatRoom.Instance;
        if (room is null)
        {
            return;
        }
        Tween tween = room.CreateTween();
        tween.TweenProperty(node, "global_position:x", targetX, MoveDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
        await tween.AwaitFinished(room);
    }
}
