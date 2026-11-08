using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.HoverTips;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Cards;
[RegisterCard(typeof(EventCardPool))]
public class TonyDefend : NewsanguoCardTemplate
{
    public TonyDefend() : base(1, CardType.Skill, CardRarity.Event, TargetType.Self) { }
    public override bool GainsBlock => true;
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://newsanguo/images/cards/TonyDefend.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(15m, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<TonyDefendPower>()];
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<TonyDefendPower>(context, Owner.Creature, 1m, Owner.Creature, this);
        if (Owner.Creature.Block > 15)
            await CreatureCmd.LoseBlock(context, Owner.Creature, Owner.Creature.Block - 15, Owner.Creature);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play, fast: false);
    }
}
