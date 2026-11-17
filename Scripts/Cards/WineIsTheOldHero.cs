using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Interop;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class WineIsTheOldHero : NewsanguoCardTemplate
{
    public override bool GainsBlock => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier((card, _) =>
        {
            var owner = card.Owner?.Creature;
            return SunqianUniverseVigorInterop.UsesVigor(owner)
                ? owner?.GetPowerAmount<VigorPower>() ?? 0
                : owner?.GetPowerAmount<DrunkenMightPower>() ?? 0;
        })
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        SunqianUniverseVigorInterop.ActiveResourceHoverTips(this);

    public WineIsTheOldHero() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        SunqianUniverseVigorInterop.AddActiveResourceName(description, this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/wine_is_the_old_hero");
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.CalculatedBlock.Calculate(null),
            DynamicVars.CalculatedBlock.Props, cardPlay);
        await PowerCmd.Apply<WineIsTheOldHeroPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
