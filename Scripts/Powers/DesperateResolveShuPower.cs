using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class DesperateResolveShuPower : ModPowerTemplate
{
    private readonly Dictionary<PowerModel, int> _before = new();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png");

    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target,
        Creature? applier, CardModel? cardSource)
    {
        if (target == Owner)
            _before[power] = power.Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power,
        decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner || !_before.Remove(power, out int before)) return;
        bool gainedDebuff = newsanguo.Scripts.Patches.ZilongDebuffHistoryPatch.IsGainedDebuff(power, amount, before);
        if (gainedDebuff && Owner.IsAlive)
            await PowerCmd.Apply<DamageBoostPower>(choiceContext, Owner, 10m * Amount, Owner, cardSource);
    }
}
