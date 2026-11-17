using newsanguo.Scripts.Settings;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using newsanguo.Scripts.Patches;
using MegaCrit.Sts2.Core.Combat;
using newsanguo.Scripts.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
namespace newsanguo.Scripts.Cards;
[RegisterCard(typeof(TokenCardPool))]
public class InvincibleWithinThreeHundredLi : TerritoryCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/InvincibleWithinThreeHundredLi.png");
    protected override bool ShouldGlowGoldInternal =>
        IsMutable && Pile?.Type == PileType.Hand &&
        Owner.PlayerCombatState?.Phase == PlayerTurnPhase.Play &&
        !CombatManager.Instance.IsOverOrEnding && TerritoryHandOrder.CanReposition(this);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ReplayAmount", 1m)];
    public int GrantedReplay => DynamicVars["ReplayAmount"].IntValue;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => BalanceAdjustments.Enabled
        ? base.AdditionalHoverTips
        : [..base.AdditionalHoverTips, HoverTipFactory.Static(StaticHoverTip.ReplayDynamic, new DynamicVar("Times", GrantedReplay))];

    protected override void OnUpgrade()
    {
        if (!BalanceAdjustments.Enabled)
            DynamicVars["ReplayAmount"].UpgradeValueBy(1m);
    }

    public InvincibleWithinThreeHundredLi() : base(-1, CardRarity.Token) { }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        BalanceAdjustments.Enabled && Pile?.Type == PileType.Hand && cardSource is not null &&
        TerritoryDamageSnapshotPatch.GetNeighbor(cardSource) == this ? 2m : 1m;

    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        if (cardSource is not null)
        {
            TerritoryDamageSnapshotPatch.PlaySound(cardSource, this);
        }
        return Task.CompletedTask;
    }
}
