using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

/// <summary>
/// 「顺应天意」——**选择卡**，不是可打出的牌：
/// 只在 <see cref="HeavensForcePower"/> 回合结束询问“要不要转化”时，作为
/// <c>CardSelectCmd.FromChooseACardScreen</c> 的二选一候选出现（照抄原版「知识恶魔」的
/// 「知识的诅咒」：KnowledgeDemon.ChooseCurse 用 CombatState.CreateCard 造两张卡让玩家选）。
/// 玩家选它 = 接受这次正向转化；真正的效果由 <see cref="HeavensForcePower"/> 执行，
/// 本卡只负责界面与文案（因此费用 -1、不可能被打出，也不会真正进入任何牌堆）。
/// </summary>
[RegisterCard(typeof(StatusCardPool))]
public class HeavensForceAccept : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 状态牌：不能升级、不参与随机生成
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    // 属于“天意”体系（与 HeavensDecay 等状态/诅咒牌一致）
    public override bool IsHeavensCard => true;

    // 文案里的 {ForceCost} 直接引用能力侧的常量，改数值只需改 HeavensForcePower 一处
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("ForceCost", HeavensForcePower.ConversionAmount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<DoubleDamagePower>()
    ];

    public HeavensForceAccept() : base(-1, CardType.Status, CardRarity.Status, TargetType.None)
    {
    }
}
