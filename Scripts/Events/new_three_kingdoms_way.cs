using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

/// <summary>
/// 新三国道（第二幕事件）：
/// 选项1：获得遗物「传送门」；
/// 选项2：被传送到这一幕的先古之民处。
/// </summary>
[RegisterActEvent(typeof(Hive))]
public class new_three_kingdoms_way : ModEventTemplate
{
    // 选项2会切换到其它房间（先古事件），按游戏要求所有切换房间的事件必须是共享事件
    public override bool IsShared => true;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"res://newsanguo/images/events/{GetType().Name}.png"
    );

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            CreateModRelicOption<portal>(OnGainPortal),
            new EventOption(this, GoToAncient, InitialOptionKey("GO_TO_ANCIENT"))
        ];
    }

    private async Task OnGainPortal()
    {
        await RelicCmd.Obtain<portal>(Owner!);
        SetEventFinished(PageDescription("GAIN_PORTAL"));
    }

    private async Task GoToAncient()
    {
        IRunState runState = Owner!.RunState;
        // 多人下共享事件的选项回调会遍历所有玩家的克隆执行：加牌与传送都只在"本机玩家"
        // 的克隆上执行一次，保证每个玩家只获得一张诅咒、房间只切换一次。
        if (LocalContext.IsMe(Owner))
        {
            // 获得诅咒「天意侵蚀」并加入牌组
            CardModel curse = runState.CreateCard<heavens_decay>(Owner!);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(curse, PileType.Deck));
        }
        // 先标记事件结束，让玩家离开当前事件房间时一切正常
        SetEventFinished(PageDescription("GO_TO_ANCIENT"));
        // 不能在选项回调内直接 await 房间切换：选项任务会被 EventSynchronizer.AwaitPendingOptionTasks
        // 等待，而 EnterRoom 又会先 ExitCurrentRooms -> 等待同样的任务，造成死锁。
        // 因此把切换调度到选项任务结算完成之后（延迟后）再执行。
        if (LocalContext.IsMe(Owner))
        {
            _ = TaskHelper.RunSafely(EnterAncientAsync(runState));
        }
    }

    private async Task EnterAncientAsync(IRunState runState)
    {
        await Task.Delay(200);
        // 让地图"当前位置"落到先古节点：先清空访问记录（先古坐标开局时已访问过，若不清空，
        // EnterMapCoord 里的 AddVisitedMapCoord 会因坐标已存在而直接返回，无法进入），
        // 再从起始点（先古）走标准进入流程。进入后 CurrentMapCoord 即先古坐标，
        // 地图可旅行点从先古的 Children（第一行）计算，因此下一个房间也从先古之民处选择。
        if (runState is RunState concreteRunState)
        {
            concreteRunState.ClearVisitedMapCoordsDebug();
        }
        await RunManager.Instance.EnterMapCoord(runState.Map.StartingMapPoint.coord);
    }
}
