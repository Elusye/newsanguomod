using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

/// <summary>
/// 野生中立伏兵（第二幕事件）：
/// 你可选择「逃跑」直接离开，或「战斗！」迎战一场艰难的战斗
/// （SkulkingColony + BygoneEffigy，精英难度）。胜利后获得标准的精英奖励
/// （金币 + 卡牌 + 随机遗物等），并额外获得 100 金币与一件随机稀有遗物，
/// 战斗结束直接进入下一地图点。
/// </summary>
[RegisterActEvent(typeof(Hive))]
public class wild_neutral_ambush : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"res://newsanguo/images/events/{GetType().Name}.png"
    );

    // 进入战斗的事件必须是共享事件（多人下由投票决定进入）
    public override bool IsShared => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, RunAway, ModOptionKey("INITIAL", "RUN_AWAY")),
            new EventOption(this, ContinueFight, ModOptionKey("INITIAL", "FIGHT"))
        ];
    }

    private Task RunAway()
    {
        // 逃跑：直接结束事件，继续地图
        SetEventFinished(PageDescription("RUN_AWAY"));
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
        EnterCombatWithoutExitingEvent<skulking_colony_effigy_encounter>(rewards, shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }
}

/// <summary>
/// 事件专属的艰难遭遇：SkulkingColony + BygoneEffigy（精英难度）。
/// ShouldGiveRewards 保持 true：胜利后发放标准精英奖励（金币 + 卡牌 + 随机遗物等），
/// 事件另经 extraRewards 额外提供 100 金币 + 一件随机稀有遗物（叠加展示）。
/// </summary>
public sealed class skulking_colony_effigy_encounter : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;

    // 注意：不能设置 HasScene => true（那会让 NCombatRoom 尝试加载 res://scenes/encounters/<id>.tscn
    // 自定义场景，文件不存在会导致敌人视觉创建中断）。同时没有自定义场景时槽位必须为 null——
    // NCombatRoom.AddCreature 遇到非 null 槽位但 EncounterSlots == null 会抛异常。保持默认
    // Slots 为空数组 + slot 为 null，走 PositionEnemies 自动布局。

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<SkulkingColony>(),
        ModelDb.Monster<BygoneEffigy>()
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<SkulkingColony>().ToMutable(), null),
            (ModelDb.Monster<BygoneEffigy>().ToMutable(), null)
        ];
    }
}
