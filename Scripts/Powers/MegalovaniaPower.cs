using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// 「狂妄之人」（MEGALOVANIA）能力：每回合的能量上限 +Amount（每层 +1 点能量）。
///
/// 实现方式参考「恭喜爹可以称帝了！」（<see cref="FatherCanClaimTheThronePower"/>）：
/// 用 <see cref="ModifyMaxEnergy"/> 做被动修正，而不是在回合开始的钩子里调 PlayerCmd.GainEnergy ——
/// 被动修正等于“本回合的能量上限本来就这么多”，不会被回合开始的重置冲掉，
/// 也没有“先给能量、再被重置”的顺序问题。
///
/// 叠加：StackType 为 Counter，Amount 就是每回合多出来的能量数（重复打出「狂妄之人」会叠层）。
/// 显示名/描述在 localization/*/powers.json 的 NEWSANGUO_POWER_MEGALOVANIA_POWER.*（名字取自类名）；
/// 图标按类名取 res://newsanguo/images/powers/MegalovaniaPower.png（大图 …MegalovaniaPowerBig.png）。
///
/// 2026-10-09：按用户要求，本能力**不再每回合闪烁图标、也不再播放音效**（原 AfterPlayerTurnStart 钩子已删除）。
/// 数值完全由下面的 ModifyMaxEnergy 被动修正承担，不依赖任何回合开始钩子，
/// 因此也不需要覆写 ShouldReceiveCombatHooks（PowerModel 默认即 true）。
/// </summary>
[RegisterPower]
public class MegalovaniaPower : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount = 每回合多出来的能量数（每打出一张「狂妄之人」+1）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 悬停提示：展示“能量”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.ForEnergy(this)];

    // 每回合的能量上限 + 层数（= 每回合多拿 Amount 点能量）
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner.Player)
        {
            return amount;
        }
        return amount + Amount;
    }
}
