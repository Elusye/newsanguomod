using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using newsanguo.Scripts.Combat;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using newsanguo.Scripts.Characters;
using MegaCrit.Sts2.Core.Models.CardPools;
namespace newsanguo.Scripts.Cards;
[RegisterCard(typeof(TokenCardPool))]
public class InvincibleWithinThreeHundredLi : TerritoryCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/InvincibleWithinThreeHundredLi.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ReplayAmount", 1m)];
    public int GrantedReplay => DynamicVars["ReplayAmount"].IntValue;
    protected override bool ShouldGlowGoldInternal =>
        IsMutable && Pile?.Type == PileType.Hand &&
        Owner.PlayerCombatState?.Phase == PlayerTurnPhase.Play &&
        !CombatManager.Instance.IsOverOrEnding && TerritoryHandOrder.CanReposition(this);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. base.AdditionalHoverTips,
        HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, new DynamicVar("Times", GrantedReplay))
    ];
    public InvincibleWithinThreeHundredLi() : base(-1, CardRarity.Token) { }
    protected override void OnUpgrade() => DynamicVars["ReplayAmount"].UpgradeValueBy(1m);
}
