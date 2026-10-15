using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class SwitchAllegiancePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png");

    // 此钩子早于战斗能力清理；胜利钩子全部完成后由转换补丁执行。
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Player is { } player)
            AllegianceChange.Schedule(player);
        return Task.CompletedTask;
    }
}
