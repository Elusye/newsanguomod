using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.HoverTips;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class SwitchAllegiance : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;


    public SwitchAllegiance() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/switch_allegiance");
        await PowerCmd.Apply<SwitchAllegiancePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        // 只移除这张战斗牌对应的牌组本体；衍生牌和复制品不删除其他同名牌。
        if (DeckVersion is { } deckCard && deckCard.Pile?.Type == PileType.Deck)
            await CardPileCmd.RemoveFromDeck(deckCard, showPreview: false);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
    protected override void AfterDowngraded() => RemoveKeyword(CardKeyword.Innate);
}
