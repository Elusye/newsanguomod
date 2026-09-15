using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “抽牌变少”：下回合少抽 Amount 张牌，随后自动移除。
/// 写法照原版 DrawCardsNextTurnPower，唯一区别是把“增加抽牌数”改成“减少抽牌数”。
/// </summary>
[RegisterPower]
public class DrawReductionPower : ModPowerTemplate
{
    // 负面效果：Debuff（与力量为负时的口径一致，可被“移除负面效果”类卡牌清除）
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：计数器，层数 = 下回合少抽的牌数
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数：层数即减少的抽牌数，恒为正
    public override bool AllowNegative => false;
    // 需要战斗钩子（抽牌修正、回合开始移除）
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 抽牌时减少对应张数（AmountOnTurnStart 保证只对“施加时就已存在”的层数生效）
    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (player != Owner.Player)
        {
            return count;
        }
        if (AmountOnTurnStart == 0)
        {
            return count;
        }
        return count - Amount;
    }

    // 玩家回合开始时移除自身（与原版“下回合抽牌”一致）
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner) && AmountOnTurnStart != 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
