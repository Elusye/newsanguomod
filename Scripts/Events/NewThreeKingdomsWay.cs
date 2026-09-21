using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
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
    // 选项2的遗物会切换到其它房间（先古事件），按游戏要求所有切换房间的事件必须是共享事件。
    // 本事件现在只在单人局出现（见下方 IsAllowed），所以这里保持共享写法：
    // 共享事件在单人是无差异路径，而万一被控制台/存档强制拉进多人局，
    // 走的仍是那套已修好的“共享事件选项回调在每台机器上遍历所有玩家克隆”的逻辑，不会因为房间切换而分歧。
    public override bool IsShared => true;

    // 单人事件：多人局不再抽取本事件。
    // 理由：（1）选项1给的「传送门」本身已改为单人限定（见 Portal.IsAllowed）；
    //      （2）选项2的「R 键」会切换房间（回到先古之民处），而房间切换在多人局属于全局操作，
    //           非共享事件在多人下每台机器只跑自己的克隆，切换请求会算不出一致的唯一执行者。
    // 事件抽取时会走 RoomSet.EnsureNextEventIsValid → NextEvent.IsAllowed(runState)（RoomSet.cs:118），
    // 所以 IsAllowed 返回 false 就能让本事件在多人局彻底不入池。
    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

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
