using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Factories;
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

    public Flourishing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/flourishing");
        CardModel[] pool =
        [
            ModelDb.Card<MindControlSpell>(),
            ModelDb.Card<AntiMagicField>(),
            ModelDb.Card<BowangSlopeParadox>(),
            ModelDb.Card<AutoPilot>(),
            ModelDb.Card<Hanzhong>(),
            ModelDb.Card<ZhouYafu>(),
            ModelDb.Card<SoImpressive>(),
            ModelDb.Card<NoReturn>(),
            ModelDb.Card<MyThreeGenerals>()
        ];
        List<CardModel> options = CardFactory.GetDistinctForCombat(
            Owner, pool, 3, Owner.RunState.Rng.CombatCardGeneration).ToList();
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, options, Owner, canSkip: false);
        if (selected is null)
            return;

        if (selected is not TerritoryCardTemplate)
            selected.SetToFreeThisTurn();

        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);

    protected override void AfterDowngraded() => AddKeyword(CardKeyword.Exhaust);
}
