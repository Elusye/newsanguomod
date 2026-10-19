using System.Collections.Generic;
using STS2RitsuLib.Keywords;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using newsanguo.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class Jingxiang : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/Jingxiang.png");
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<HeavenAndEarth>(IsUpgraded), ModKeywordRegistry.CreateHoverTip("NEWSANGUO_KEYWORD_TERRITORY"), HoverTipFactory.FromPower<FlightPower>()];
    public Jingxiang() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<FlightPower>(context, Owner.Creature, 1m, Owner.Creature, this);
        var territory = CombatState!.CreateCard<HeavenAndEarth>(Owner);
        if (IsUpgraded)
        {
            territory.UpgradeInternal();
            territory.FinalizeUpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(territory, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
