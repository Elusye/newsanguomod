using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class DeafenMePower : ModPowerTemplate
{
    // 负面效果
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：标记（1 层即可，层数无实际含义）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 战斗结束钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount,
        Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not DeafenMePower || target != Owner || amount <= 0)
            return false;
        modifiedAmount = Math.Min(amount, Math.Max(0, 100 - Amount));
        return modifiedAmount != amount;
    }

    private void SyncVolume()
    {
        if (LocalContext.IsMe(Owner))
            HearingVolumeController.SetReduction(Math.Clamp(Amount, 0, 100));
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncVolume();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (power == this)
            SyncVolume();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (LocalContext.IsMe(oldOwner))
            HearingVolumeController.RestoreFullVolume();
        return Task.CompletedTask;
    }

    // 战斗结束时：恢复本机音量（无论战斗胜负都要恢复，否则会一直停留在音量减半状态）
    // 仅本机玩家恢复；其他玩家的机器从未被压低音量，无需处理
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (LocalContext.IsMe(Owner))
        {
            HearingVolumeController.RestoreFullVolume();
        }
        return Task.CompletedTask;
    }
}
