using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Acts;
using newsanguo.Scripts.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

/// <summary>
/// 新三国道（第二幕事件）：
/// 选项1：获得遗物「传送门」；
/// 选项2：获得遗物「R 键」（拾起时回到这一幕的先古之民处，并加入一张「天意侵蚀」）。
/// </summary>
[RegisterActEvent(typeof(Hive))]
public class NewThreeKingdomsWay : ModEventTemplate
{
    // 选项2的遗物会切换到其它房间（先古事件），按游戏要求所有切换房间的事件必须是共享事件
    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"res://newsanguo/images/events/{GetType().Name}.png"
    );

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        EventOption portalOption = CreateModRelicOption<Portal>(OnGainPortal);
        // 悬停时显示“传送门”遗物信息（遗物预览 + 说明）
        portalOption.HoverTips = HoverTipFactory.FromRelic<Portal>();

        EventOption rKeyOption = CreateModRelicOption<RKey>(OnGainRKey);
        // 悬停时显示“R 键”遗物信息（遗物预览 + 说明）
        rKeyOption.HoverTips = HoverTipFactory.FromRelic<RKey>();

        return [portalOption, rKeyOption];
    }

    private async Task OnGainPortal()
    {
        await RelicCmd.Obtain<Portal>(Owner!);
        SetEventFinished(PageDescription("GAIN_PORTAL"));
    }

    // R 键的拾起效果（加诅咒 + 回到先古之民处）由遗物自身实现
    private async Task OnGainRKey()
    {
        await RelicCmd.Obtain<RKey>(Owner!);
        SetEventFinished(PageDescription("GO_TO_ANCIENT"));
    }
}
