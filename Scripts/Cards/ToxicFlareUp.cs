using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(CurseCardPool))]
public sealed class ToxicFlareUp : NewsanguoCurseTemplate
{
    public ToxicFlareUp() : base(-1) { }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/ToxicFlareUp.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Unplayable, CardKeyword.Ethereal, .. base.CanonicalKeywords];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw) =>
        card == this ? PlayerCmd.LoseEnergy(DynamicVars.Energy.IntValue, Owner) : Task.CompletedTask;
}
