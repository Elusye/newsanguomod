using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using newsanguo.Scripts.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Relics;

// 传送门：你下 3 次选择下一层的房间时可以无视当前的路线（机制参照原版 winged_boots）
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class Portal : ModRelicTemplate
{
    // 可无视路线的次数上限
    private const int _roomCount = 3;

    private int _timesUsed;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Event;

    // 单人限定，与原版同效果的「飞靴」一致（WingedBoots.cs:47-50 的 IsAllowed）。
    // 多人下自由移动有两个硬伤：
    //   1) RunManager.EnterMapPointInternal 每次进点都会重新 RollRoomTypeFor 摇房间类型（RunManager.cs:840），
    //      于是允许「重访已走过的坐标」，同一个坐标第二次可能变成宝箱房；
    //   2) 原版多人宝箱房的选遗物会话在“没结算就离房”时不会被清理
    //      （TreasureRoomRelicSynchronizer.cs:302-308 只在单人跳过时 EndRelicVoting），
    //      下一次进宝箱房 BeginRelicPicking() 会抛 InvalidOperationException，而该异常发生在
    //      FadeOut 之后、FadeIn 与 ActionExecutor.Unpause() 之前 → 黑屏且输入队列永久暂停。
    // 因此多人局不发放「传送门」，把这条可重访节点的路径直接掐掉。
    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1;
    }

    // 剩余次数用完后遗物失效
    public override bool IsUsedUp => TimesUsed >= _roomCount;

    // 用完后不再显示角标
    public override bool ShowCounter => !IsUsedUp;

    // 图标角标：剩余可无视路线的次数
    public override int DisplayAmount => _roomCount - TimesUsed;

    // 描述中的 {Rooms}：随剩余次数动态变化
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Rooms", 3m)];

    // 已使用的次数（存档属性，v0.111.0 下由 ModelDb 自动收录）
    [SavedProperty]
    public int TimesUsed
    {
        get => _timesUsed;
        set
        {
            AssertMutable();
            _timesUsed = value;
            base.DynamicVars["Rooms"].BaseValue = _roomCount - _timesUsed;
            InvokeDisplayAmountChanged();
            CheckIfUsedUp();
        }
    }

    // 剩余次数未用完时允许无视路线（自由移动）
    public override bool ShouldAllowFreeTravel()
    {
        return !IsUsedUp;
    }

    // 进入房间后判定：无论到达的点是否是上一格房间的子节点，都消耗一次“无视路线”的次数
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (IsUsedUp)
        {
            return Task.CompletedTask;
        }
        if (base.Owner.RunState.CurrentRoomCount > 1)
        {
            return Task.CompletedTask;
        }
        if (!(base.Owner.RunState is RunState runState))
        {
            return Task.CompletedTask;
        }
        if (runState.VisitedMapCoords.Count <= 1)
        {
            return Task.CompletedTask;
        }
        TimesUsed++;
        return Task.CompletedTask;
    }

    private void CheckIfUsedUp()
    {
        if (IsUsedUp)
        {
            base.Status = RelicStatus.Disabled;
        }
    }
}
