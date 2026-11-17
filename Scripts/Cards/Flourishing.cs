using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class Flourishing : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/Flourishing.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    public Flourishing() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/flourishing");
        CardModel[] pool =
        [
            ModelDb.Card<MindControlSpell>(),
            ModelDb.Card<AntiMagicField>(),
            ModelDb.Card<BowangSlopeParadox>(),
            ModelDb.Card<AutoPilot>(),
            ModelDb.Card<SunBen>(),
            ModelDb.Card<ZhouYafu>(),
            ModelDb.Card<SoImpressive>(),
            ModelDb.Card<NoReturn>(),
            ModelDb.Card<MyThreeGenerals>()
        ];
        int index = Owner.RunState.Rng.CombatCardGeneration.NextInt(pool.Length);
        var generated = CombatState!.CreateCard(pool[index], Owner);
        if (generated is not TerritoryCardTemplate)
            generated.SetToFreeThisTurn();

        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
