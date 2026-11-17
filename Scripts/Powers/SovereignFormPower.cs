using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Interop;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class SovereignFormPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            SunqianUniverseVigorInterop.AddActiveResourceName(description, this);
            return description;
        }
    }

    // 记录当前联动资源（酒力 / 活力）变化前的层数
    private int _resourceAmountBeforeChange;

    // 在酒力层数变化前记录旧值
    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return Task.CompletedTask;
        if (target == Owner && SunqianUniverseVigorInterop.IsActiveResourcePower(power, Owner))
        {
            _resourceAmountBeforeChange = power.Amount;
        }
        return Task.CompletedTask;
    }

    // 酒力真的增加后，获得天意之力
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        // 只认本方身上的酒力（失去酒力时 amount 为负，直接放行不入账）
        if (power.Owner != Owner || !SunqianUniverseVigorInterop.IsActiveResourcePower(power, Owner)) return;
        if (amount <= 0m) return;

        int gainedAmount = power.Amount - _resourceAmountBeforeChange;
        if (gainedAmount <= 0) return;

        await HeavensForce.Add(choiceContext, Owner.Player, Amount, null);
    }
}
