using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “聋”：本场战斗中你不能再听到任何声音。
/// 打出“扎聋我自己的耳朵！”后静音所有音效，战斗结束时自动恢复。
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

    // 战斗结束时：恢复所有音效（无论战斗胜负都要恢复，否则会一直静音）
    // 仅本机玩家恢复；其他玩家的机器从未被静音，无需处理
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (LocalContext.IsMe(Owner))
        {
            FmodStudioMixerGlobals.TryUnmuteAllEvents();
        }
        return Task.CompletedTask;
    }
}
