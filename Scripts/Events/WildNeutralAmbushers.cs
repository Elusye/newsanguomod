using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

[RegisterActEvent(typeof(Hive))]
public class WildNeutralAmbushers : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"res://newsanguo/images/events/{GetType().Name}.png"
    );

    // 进入战斗的事件必须是共享事件（多人下由投票决定进入）
    public override bool IsShared => true;

    public override bool IsAllowed(IRunState runState) =>
        newsanguo.Scripts.Settings.NewsanguoEventSettings.IsEnabled(runState) &&
        runState.Act is Hive && runState.ActFloor > runState.Map.GetRowCount() / 2;

    // 与陈留大食堂一致，在进入事件时播放入场语音。
    public override Task AfterEventStarted()
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/wild_neutral_ambushers");
        return Task.CompletedTask;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() =>
        [new EventOption(this, FightFirst, ModOptionKey("INITIAL", "FIGHT"))];

    private Task FightFirst()
    {
        EnterCombatWithoutExitingEvent<WildNeutralAmbushersFirstEncounter>([], shouldResumeAfterCombat: true);
        return Task.CompletedTask;
    }

    public override Task Resume(AbstractRoom exitedRoom)
    {
        if (exitedRoom is not CombatRoom { Encounter: WildNeutralAmbushersFirstEncounter })
            return base.Resume(exitedRoom);

        _combatSynchronizer?.ResetState();
        _combatSynchronizer?.InitializeForEvent(this);
        SetEventState(PageDescription("AFTER_FIRST"),
        [
            new EventOption(this, RunAwayAfterFirst, ModOptionKey("AFTER_FIRST", "RUN_AWAY")),
            new EventOption(this, ContinueFight, ModOptionKey("AFTER_FIRST", "FIGHT"))
        ]);
        return Task.CompletedTask;
    }

    private Task RunAwayAfterFirst()
    {
        SetEventFinished(PageDescription("AFTER_FIRST_RUN_AWAY"));
        return Task.CompletedTask;
    }

    private Task ContinueFight()
    {
        // 战斗：进入艰难的战斗（精英难度）。胜利后获得标准的精英奖励（金币 + 卡牌 + 随机遗物等），
        // 并额外获得 100 金币与一件随机稀有遗物；战斗结束直接继续地图（事件没有后续选项）。
        Player player = Owner!;
        IReadOnlyList<Reward> rewards =
        [
            new GoldReward(100, player),
            new RelicReward(RelicRarity.Rare, player)
        ];
        EnterCombatWithoutExitingEvent<SkulkingColonyEffigyEncounter>(rewards, shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }
}

// 复用原版双盛碗虫遭遇的场景和槽位，保留伏兵事件的首战阵容。
public sealed class WildNeutralAmbushersFirstEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Monster;
    public override bool ShouldGiveRewards => false;
    public override bool HasScene => true;
    protected override string ScenePath => "res://scenes/encounters/bowlbugs_weak.tscn";
    public override float GetCameraScaling() => ModelDb.Encounter<BowlbugsWeak>().GetCameraScaling();
    public override Godot.Vector2 GetCameraOffset() => ModelDb.Encounter<BowlbugsWeak>().GetCameraOffset();

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<BowlbugRock>(), ModelDb.Monster<BowlbugSilk>()];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<BowlbugRock>().ToMutable(), "odd"),
        (ModelDb.Monster<BowlbugSilk>().ToMutable(), "even")
    ];
}

/// <summary>
/// 事件专属的艰难遭遇：SlumberingBeetle + BygoneEffigy（精英难度）。
/// ShouldGiveRewards 保持 true：胜利后发放标准精英奖励（金币 + 卡牌 + 随机遗物等），
/// 事件另经 extraRewards 额外提供 100 金币 + 一件随机稀有遗物（叠加展示）。
/// </summary>
public sealed class SkulkingColonyEffigyEncounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;

    // 与原版 OvicopterNormal 一致，扩大镜头范围，缓解两只大型敌人的场面拥挤。
    public override float GetCameraScaling() => 0.8f;
    public override Godot.Vector2 GetCameraOffset() =>
        Godot.Vector2.Down * 50f + Godot.Vector2.Left * 100f;


    // 注意：不能设置 HasScene => true（那会让 NCombatRoom 尝试加载 res://scenes/encounters/<id>.tscn
    // 自定义场景，文件不存在会导致敌人视觉创建中断）。同时没有自定义场景时槽位必须为 null——
    // NCombatRoom.AddCreature 遇到非 null 槽位但 EncounterSlots == null 会抛异常。保持默认
    // Slots 为空数组 + slot 为 null，走 PositionEnemies 自动布局。

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SlumberingBeetle>(),
        ModelDb.Monster<BygoneEffigy>()
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<BygoneEffigy>().ToMutable(), null),
            (ModelDb.Monster<SlumberingBeetle>().ToMutable(), null)
        ];
    }
}
