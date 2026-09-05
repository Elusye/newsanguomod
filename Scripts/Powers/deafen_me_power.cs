using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “听觉受损”：本场战斗中你听到的声音音量降低 50%。
/// 打出“扎聋我自己的耳朵！”后压低本机音量，战斗结束时自动恢复。
/// </summary>
[RegisterPower]
public class deafen_me_power : ModPowerTemplate
{
    // 负面效果
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：标记（1 层即可，层数无实际含义）
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    // 战斗结束钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

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
