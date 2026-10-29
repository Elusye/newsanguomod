using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class DontBeAngry : NewsanguoCardTemplate
{
    public DontBeAngry() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/DontBeAngry.png");
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.Static(StaticHoverTip.Block)];
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2m);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/dont_be_angry");
        var attacks = PileType.Hand.GetPile(Owner).Cards.Where(card => card.Type == CardType.Attack).ToList();
        foreach (var card in attacks)
        {
            if (card.Pile?.Type != PileType.Hand) continue;
            if (await CardCmd.Exhaust(context, card) is not null)
                await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play, fast: false);
        }
        await PowerCmd.Apply<DontBeAngryPower>(context, Owner.Creature, 1m, Owner.Creature, this);
    }
}
