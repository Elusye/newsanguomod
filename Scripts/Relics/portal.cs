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
public class portal : ModRelicTemplate
{
    // 可无视路线的次数上限
    private const int _roomCount = 3;

    private int _timesUsed;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}_outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}_big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Event;

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
