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
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class Hanzhong : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/Hanzhong.png");
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<AntiMagicField>(), HoverTipFactory.FromPower<IronCavalryPower>(), ModKeywordRegistry.CreateHoverTip("NEWSANGUO_KEYWORD_TERRITORY")];
    public Hanzhong() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/hanzhong");
        var territory = CombatState!.CreateCard<AntiMagicField>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(territory, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
