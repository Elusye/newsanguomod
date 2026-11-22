using newsanguo.Scripts.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class LeaveItToFate : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<TaintedPower>(3m), new EnergyVar(2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<TaintedPower>(), HoverTipFactory.ForEnergy(this)];

    public LeaveItToFate() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/leave_it_to_fate");
        await PowerCmd.Apply<TaintedPower>(choiceContext, Owner.Creature,
            DynamicVars["TaintedPower"].IntValue, Owner.Creature, this);

        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}
