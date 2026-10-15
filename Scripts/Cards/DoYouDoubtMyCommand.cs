using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class DoYouDoubtMyCommand : NewsanguoCardTemplate
{
    private CardModel? _selected;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];
    public DoYouDoubtMyCommand() : base(4, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/do_you_doubt_my_command");
        CardPile draw = PileType.Draw.GetPile(Owner);
        if (draw.Cards.Count == 0) return;
        _selected = (await CardSelectCmd.FromCombatPile(choiceContext, draw, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (_selected is null) return;
        try
        {
            // 使用同一张原牌的重放流程，两次结算后仅消耗一次；自动选择攻击目标。
            await CardCmd.AutoPlay(choiceContext, _selected, null);
        }
        finally
        {
            _selected = null;
        }
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) =>
        card == _selected ? playCount + 1 : playCount;

    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay,
        ResourceInfo resources, CardLocation cardLocation) => card == _selected
        ? new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom)
        : cardLocation;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
