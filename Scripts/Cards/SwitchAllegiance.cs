using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using newsanguo.Scripts.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class SwitchAllegiance : NewsanguoCardTemplate
{
    public SwitchAllegiance() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/SwitchAllegiance.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HeavensForce.HoverTip()];
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/switch_allegiance");
        int maximum = Math.Min(DynamicVars.Cards.IntValue, PileType.Hand.GetPile(Owner).Cards.Count);
        if (maximum == 0) return;
        var selected = (await CardSelectCmd.FromHand(context: context, player: Owner,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, maximum),
            filter: null, source: this)).ToList();
        int exhausted = 0;
        foreach (var card in selected)
        {
            if (card.Pile?.Type != PileType.Hand) continue;
            await CardCmd.Exhaust(context, card);
            exhausted++;
        }
        if (exhausted == 0) return;
        var pool = ModelDb.CardPool<NewsanguoCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint);
        var generated = CardFactory.GetForCombat(Owner, pool, exhausted,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        foreach (var card in generated)
        {
            if (card is NewsanguoCardTemplate modCard) modCard.SetToFreeHeavensForceForCombat();
            var result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
            CardCmd.PreviewCardPileAdd(result, 1.2f);
        }
    }
}
