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
    protected override IEnumerable<DynamicVar> CanonicalVars => BalanceAdjustments.Enabled ? [
        new PowerVar<VulnerablePower>(1m), new PowerVar<WeakPower>(1m), new EnergyVar(2)
    ] : [new PowerVar<FrailPower>(1m), new EnergyVar(2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => BalanceAdjustments.Enabled ? [
        HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>(), HoverTipFactory.ForEnergy(this)
    ] : [HoverTipFactory.FromPower<FrailPower>(), HoverTipFactory.ForEnergy(this)];
    public override CardRarity Rarity => BalanceAdjustments.Enabled ? CardRarity.Uncommon : CardRarity.Common;

    public LeaveItToFate() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/leave_it_to_fate");
        if (BalanceAdjustments.Enabled)
        {
            // 当轮敌方回合结束即递减。
            var vulnerable = await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature,
                DynamicVars["VulnerablePower"].IntValue, Owner.Creature, this);
            if (vulnerable is not null)
                vulnerable.SkipNextDurationTick = false;

            var weak = await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature,
                DynamicVars["WeakPower"].IntValue, Owner.Creature, this);
            if (weak is not null)
                weak.SkipNextDurationTick = false;
        }
        else
        {
            var frail = await PowerCmd.Apply<FrailPower>(choiceContext, Owner.Creature,
                DynamicVars["FrailPower"].IntValue, Owner.Creature, this);
            if (frail is not null)
                frail.SkipNextDurationTick = false;
        }

        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}
