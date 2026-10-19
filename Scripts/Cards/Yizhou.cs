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
public class Yizhou : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/Yizhou.png");
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<EndlessEnergy>(IsUpgraded), ModKeywordRegistry.CreateHoverTip("NEWSANGUO_KEYWORD_TERRITORY")];
    public Yizhou() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var territory = CombatState!.CreateCard<EndlessEnergy>(Owner);
        if (IsUpgraded)
        {
            territory.UpgradeInternal();
            territory.FinalizeUpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(territory, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
