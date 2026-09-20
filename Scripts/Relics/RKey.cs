using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Relics;

// R 键：拾起时回到这一幕的先古之民处，并将一张「天意侵蚀」加入你的牌组。
// 逻辑与「新三国道」事件原本的“走新三国道”选项一致，现在是这个遗物来承担。
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class RKey : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Event;

    // 效果只在拾起时生效
    public override bool HasUponPickupEffect => true;

    // 悬停时展示诅咒「天意侵蚀」（含其自身的悬浮提示）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. HoverTipFactory.FromCardWithCardHoverTips<HeavensDecay>()
    ];

    public override async Task AfterObtained()
    {
        IRunState runState = Owner.RunState;

        // 每个玩家各自获得一张诅咒「天意侵蚀」并加入自己牌组。
        // 注意：共享事件的选项回调在**每台机器**上都会遍历所有玩家的克隆执行（见 EventSynchronizer），
        // 所以这里必须让每个克隆都作用于自己的 Owner，原版共享事件（如 MorphicGrove）也是这个写法。
        // 早期版本用 LocalContext.IsMe 只让“本机玩家”的克隆执行，结果两台机器把诅咒加进了不同玩家的
        // 牌组，状态校验和分歧、客户端掉线（与「孩儿不孝啊！」属于同一类问题）。
        CardModel curse = runState.CreateCard<HeavensDecay>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(curse, PileType.Deck));

        // 切换房间是全局操作，整体只能触发一次：交给 0 号位玩家的克隆执行。
        // 这里同样不能用 LocalContext.IsMe（各机器结果不同），用玩家槽位才能保证
        // 所有机器算出的“唯一执行者”是同一个人。
        if (runState.GetPlayerSlotIndex(Owner) != 0)
        {
            return;
        }

        // 不能在拾起过程中直接 await 房间切换：此刻仍处于事件选项任务里，选项任务会被
        // EventSynchronizer.AwaitPendingOptionTasks 等待，而 EnterRoom 又会先 ExitCurrentRooms
        // -> 等待同样的任务，造成死锁。因此把切换调度到选项任务结算完成之后（延迟后）再执行。
        _ = TaskHelper.RunSafely(EnterAncientAsync(runState));
    }

    private async Task EnterAncientAsync(IRunState runState)
    {
        await Task.Delay(200);
        // 让地图“当前位置”落到先古节点：先清空访问记录（先古坐标开局时已访问过，若不清空，
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
