using System.Collections.Generic;
using STS2RitsuLib.Keywords;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class Gucheng : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/Gucheng.png");
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust, .. base.CanonicalKeywords];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<InvincibleWithinThreeHundredLi>(), ModKeywordRegistry.CreateHoverTip("NEWSANGUO_KEYWORD_TERRITORY")];
    public Gucheng() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/gucheng");
        var territory = CombatState!.CreateCard<InvincibleWithinThreeHundredLi>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(territory, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
    protected override void AfterDowngraded() => AddKeyword(CardKeyword.Ethereal);
}
