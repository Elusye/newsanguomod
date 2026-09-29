using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using newsanguo.Scripts.Monsters;
using newsanguo.Scripts.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

/// <summary>
/// 实践主义者（Pragmatist）：路遇遇难冒险者的行囊，翻找战利品。
///
/// 流程：
///  · 「搜索」：按 {35% / 60% / 85%}（第 1/2/3 次）判定“怪物回来了”——命中则被偷袭，只能「战斗」，
///    与本层的精英怪交战（战斗胜利后事件结束，直接前往下一个地图点）；
///  · 未命中则从尚未获得的奖励中随机取一种：30 金币 / 什么都没有 / 一件随机遗物（每种最多一次）；
///    结果页给出「继续」（进入下一次搜索，概率更高）与「离开」；
///  · 三次搜索用尽（或奖励拿完）后只能「离开」；最后一次若恰好是“什么都没有”，
///    走「翻遍了他的所有物，什么事也没发生」这一页。
///
/// 「巨石」遗物**只在击败盛碗虫（巨石）本尊**的伏击战后掉落：只有巢穴（Hive，第二幕）的伏击
/// 才有 BowlbugChancePercent%（25%）概率碰上它，其余情况（以及其它幕）打的是本幕随机精英，
/// 只给标准精英奖励、不掉「巨石」。
/// 判定见 PragmatistRewardPatch（奖励替换）与 Fight() 里的 extraRewards 兜底。
///
/// 会进入战斗，所以必须是共享事件（EventModel.EnterCombatWithoutExitingEvent 里强制校验 IsShared）。
/// 布局用 Combat（无立绘，直接以当前战斗场景为背景，场上站着本层的精英怪），见下面的 LayoutType 注释。
/// </summary>
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public class Pragmatist : ModEventTemplate
{
    // 第 1/2/3 次搜索时“怪物回来”的概率（%）
    private const int FirstAmbushPercent = 35;
    private const int SecondAmbushPercent = 60;
    private const int ThirdAmbushPercent = 85;

    // 搜索最多进行三次（对应三档概率）
    private const int MaxSearches = 3;

    // 巢穴（Hive，第二幕）的伏击里，对手是「盛碗虫（巨石）」的概率（%）；未命中则打本幕随机精英。
    // 掷骰用事件自己的 Rng：种子 = 本局种子 + 事件 id 的确定性哈希，共享事件不加玩家槽位
    // （EventModel.cs:238），因此每台机器、每个玩家克隆的序列完全一致；配合下面的“只掷一次”缓存，
    // 多人下所有客户端掷出的结果必然相同。
    private const double BowlbugChancePercent = 25.0;

    // 本次事件掷出的伏击遭遇（非存档字段，只是缓存：CanonicalEncounter 会被读多次）
    private EncounterModel? _ambushEncounter;

    // 搜索可能获得的三种奖励（每种最多一次）
    private enum SearchResult
    {
        Gold,
        Nothing,
        Relic
    }

    // 尚未获得的奖励
    // ⚠ 不能用 readonly：见下面 DeepCloneFields —— 每个克隆必须拿到**自己**的一份列表
    private List<SearchResult> _remaining =
    [
        SearchResult.Gold,
        SearchResult.Nothing,
        SearchResult.Relic
    ];

    // 已经搜索过几次（0 = 一次都还没搜）
    private int _searches;

    private int Searches
    {
        get => _searches;
        set
        {
            AssertMutable();
            _searches = value;
        }
    }

    /// <summary>
    /// 每次「进场」时重置本次进场的进度。
    ///
    /// 引擎为**每个玩家**从同一个 canonical 事件克隆一份可变事件
    /// （EventSynchronizer.BeginEvent: canonicalEvent.ToMutable()），而克隆用的是
    /// <c>MemberwiseClone()</c> —— **浅拷贝**：引用类型的字段会直接指向原实例的同一个对象。
    /// 引擎的 <c>EventModel.DeepCloneFields()</c> 只重建 <c>_dynamicVars</c>，不管 mod 自己的字段。
    ///
    /// 不在这里重建 <c>_remaining</c> 的后果（实机已复现）：
    ///  · 搜索消耗的奖励是从**所有克隆与 canonical 共用的那一个 List** 上扣掉的，
    ///    于是同一个进程里之后再进这个事件，列表已经是空的 → 点一次「追杀」就直接
    ///    <c>SetEventFinished("SEARCHED_EVERYTHING")</c> 结束，什么都没拿到
    ///    （而且 35% 概率会先撞上伏击，所以表现为「有时候」）；
    ///  · 多人下同一次选项会在**每个克隆**上各执行一遍 <c>Search()</c>，两个玩家就一次扣掉两个奖励。
    /// </summary>
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();

        _remaining =
        [
            SearchResult.Gold,
            SearchResult.Nothing,
            SearchResult.Relic
        ];
        _searches = 0;

        // 伏击遭遇的缓存也要清掉：克隆是从 Rng 流的第一掷决定的，
        // 各克隆的 Rng 种子相同（BeginEvent 里按「本局种子 + 事件 id 哈希」新建，不含玩家槽位），
        // 因此重新掷出的结果在所有克隆上必然一致。
        _ambushEncounter = null;
    }

    // 进入战斗的事件必须是共享事件；多人下由投票决定进入（非共享会在 EnterCombatWithoutExitingEvent 里抛异常）
    public override bool IsShared => true;

    // —— 无立绘：直接用“战斗场景”当事件背景（参考原版「互殴」PunchOff.cs:33-35）——
    // LayoutType = Combat 时，游戏用 res://scenes/events/combat_event_layout.tscn，
    // 背景资源取 NCombatRoom.AssetPaths + 遭遇自身资源（EventModel.cs:479-484），
    // 不会再去找 res://images/events/<id>.png 那张立绘，因此本事件不需要任何美术资源。
    public override EventLayoutType LayoutType => EventLayoutType.Combat;

    // Combat 布局必须给出展示用的遭遇：EventModel.cs:389 会直接 CanonicalEncounter.ToMutable()，
    // 返回 null 会在进场时抛空引用。注意：事件内开战时复用的就是这份预生成的战斗状态
    // （EventModel.cs:624），所以场上站着的和被你偷袭后打的是同一个遭遇。
    //  · 巢穴（Hive，第二幕）：有 BowlbugChancePercent% 概率是「盛碗虫（巨石）」BowlbugBoulder
    //    （新增怪，只在这个事件里登场，见 Monsters/BowlbugBoulder.cs），否则是本幕随机精英；
    //  · 其它幕：沿用“本层的精英怪”（原版 RoomSet.NextEliteEncounter，只读索引）。
    public override EncounterModel? CanonicalEncounter
    {
        get
        {
            if (Owner?.RunState is not { } runState)
            {
                return null;
            }

            // 只掷一次并缓存：这个属性会被读多次（引擎生成事件布局、Fight() 等），
            // 每次都掷的话各次读到的遭遇可能不同，多人下还会破坏引擎的引用相等校验（见 Fight() 注释）。
            if (_ambushEncounter is not null)
            {
                return _ambushEncounter;
            }

            // Rng 尚未初始化（理论上不会发生）时按“未命中”处理，避免空引用
            bool isBowlbug = runState.Act is Hive
                && (Rng is null ? 100.0 : Rng.NextDouble() * 100.0) < BowlbugChancePercent;

            _ambushEncounter = isBowlbug
                ? ModelDb.Encounter<BowlbugBoulderEncounter>()
                : runState.Act.PullNextEncounter(RoomType.Elite);

            return _ambushEncounter;
        }
    }

    // 无立绘：不提供任何自定义资源（Combat 布局只用战斗场景资源）
    public override EventAssetProfile AssetProfile => EventAssetProfile.Empty;

    // 选项描述里用到的数值：金币奖励与三档遇怪概率（改数值只需改这一处）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new GoldVar("Gold", 30),
        new DynamicVar("AmbushPercent1", FirstAmbushPercent),
        new DynamicVar("AmbushPercent2", SecondAmbushPercent),
        new DynamicVar("AmbushPercent3", ThirdAmbushPercent)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, Search, InitialOptionKey("SEARCH")),
            new EventOption(this, Leave, InitialOptionKey("LEAVE"))
        ];
    }

    // —— 让“怪物只在该出现时才出现”——
    // Combat 布局进场时就会把 CanonicalEncounter 的怪生成到场景里（NCombatEventLayout.cs:56-62
    // → NCombatRoom.Create(..., VisualOnly)），所以这里一开始就把敌人节点藏起来，
    // 只有被偷袭（AMBUSH）时才显形——与“怪物离开了、回来时偷袭你”的设定一致。
    // 拿到节点的方式与原版「互殴」PunchOff.cs:67 相同：NCombatRoom.Instance.GetCreatureNode(creature)；
    // 事件里的这个 Instance 会经 NRun.CombatRoom → NEventRoom.EmbeddedCombatRoom 解析到嵌入的战斗房间
    //（见 NRun.cs:42-51），因此事件布局下同样可用。
    public override Task AfterEventStarted()
    {
        // 布局节点在进场之后才创建，这里多刷几次确保藏住（重复设置 Visible 很便宜）
        TaskHelper.RunSafely(HideEnemiesRoutine());
        return Task.CompletedTask;
    }

    private async Task HideEnemiesRoutine()
    {
        foreach (double delay in EnemyHideDelays)
        {
            await Cmd.Wait((float)delay);
            SetEnemiesVisible(false);
        }
    }

    private static readonly double[] EnemyHideDelays = [0.05, 0.15, 0.4];

    // 切换布局里敌人节点的可见性（取不到节点时静默跳过：非战斗布局 / 已离场 / TestMode）。
    // 不直接读 EventModel 的 _combatStateForCombatLayout：那是 protected 字段，实机版本里不一定还能访问，
    // 这里改为遍历战斗房间自己的 creature 节点，按“不是玩家”筛出敌人（事件战斗布局里只有玩家与敌人两类）。
    private void SetEnemiesVisible(bool visible)
    {
        NCombatRoom? room = NCombatRoom.Instance;
        if (room is null)
        {
            return;
        }

        foreach (NCreature node in room.CreatureNodes)
        {
            if (node is null || !GodotObject.IsInstanceValid(node))
            {
                continue;
            }
            Creature? entity = node.Entity;
            if (entity is null || entity.IsPlayer)
            {
                continue;
            }
            node.Visible = visible;

            // 兜底：把「盛碗虫（巨石）」放大到 2 倍。展示用的敌人节点不经过 CombatManager，
            // 因此不会触发 MonsterModel.AfterAddedToRoom（它只在真正开打时调用）；
            // 这里与 Monster.SetupSkins 里的放大互为保险，避免“刚出现时是原版大小、点战斗才变大”。
            if (entity.Monster is BowlbugBoulder)
            {
                BowlbugBoulder.ApplyScaleToNode(node);
            }
        }
    }

    // 搜索：先掷“怪物回来”的概率；未命中则从未获得的奖励里随机取一种
    private async Task Search()
    {
        AssertMutable();

        int ambushPercent = Searches switch
        {
            0 => FirstAmbushPercent,
            1 => SecondAmbushPercent,
            _ => ThirdAmbushPercent
        };

        if (Rng.NextDouble() * 100.0 < ambushPercent)
        {
            // 被偷袭：怪物回来了 → 显形，且只能选择战斗
            SetEnemiesVisible(true);
            SetEventState(PageDescription("AMBUSH"),
            [
                new EventOption(this, Fight, ModOptionKey("AMBUSH", "FIGHT"))
            ]);
            return;
        }

        if (_remaining.Count == 0)
        {
            // 奖励已全部拿过（正常三次流程下走不到这里，兜底）
            SetEventFinished(PageDescription("SEARCHED_EVERYTHING"));
            return;
        }

        int index = Rng.NextInt(_remaining.Count);
        SearchResult result = _remaining[index];
        _remaining.RemoveAt(index);
        Searches++;

        switch (result)
        {
            case SearchResult.Gold:
                await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner!);
                ShowResultPage("FOUND_GOLD");
                return;

            case SearchResult.Relic:
                // 随机遗物：按标准稀有度概率抽取，本局不会重复出现同一件（与陈留大食堂一致）
                RelicModel relic = RelicFactory.PullNextRelicFromFront(Owner!).ToMutable();
                await RelicCmd.Obtain(relic, Owner!);
                ShowResultPage("FOUND_RELIC");
                return;

            default:
                // 最后一次搜索才翻到“什么都没有”时，用“翻遍了他的所有物”这一页
                ShowResultPage(_remaining.Count == 0 ? "SEARCHED_EVERYTHING" : "FOUND_NOTHING");
                return;
        }
    }

    // 展示搜索结果页：文案 + 后续选项（还能继续搜索时给出「继续」，否则只有「离开」）
    private void ShowResultPage(string page)
    {
        SetEventState(PageDescription(page), BuildFollowUpOptions());
    }

    private IReadOnlyList<EventOption> BuildFollowUpOptions()
    {
        if (Searches >= MaxSearches || _remaining.Count == 0)
        {
            return [LeaveOption()];
        }

        // 「继续」的文案按第几次搜索取（概率不同）：第二次用 SEARCH_2、第三次用 SEARCH_3。
        // 这两个“页名”并不对应真实页面，只用来拼出唯一的本地化键。
        string page = Searches == 1 ? "SEARCH_2" : "SEARCH_3";
        return
        [
            new EventOption(this, Search, ModOptionKey(page, "CONTINUE")),
            LeaveOption()
        ];
    }

    private EventOption LeaveOption()
    {
        return new EventOption(this, Leave, InitialOptionKey("LEAVE"));
    }

    // 战斗：与本层的精英怪交战。
    // Combat 布局下引擎复用的是 _combatStateForCombatLayout（= 上面的 CanonicalEncounter 生成的战斗状态），
    // 所以实际出场的怪以那份为准；但传进去的遭遇仍会被引擎逐个校验（见下方注释）。
    // 战斗胜利后事件结束，直接前往下一个地图点（shouldResumeAfterCombat: false；
    // Combat 布局下也必须是 false，否则 EnterCombatWithoutExitingEvent 会抛异常，EventModel.cs:614）。
    private Task Fight()
    {
        EncounterModel? elite = CanonicalEncounter;
        if (elite is null)
        {
            // 兜底：取不到本幕精英遭遇时直接结束，避免异常
            SetEventFinished(PageDescription("LEAVE"));
            return Task.CompletedTask;
        }

        // 「巨石」只在**对手就是盛碗虫（巨石）本尊**时才作为额外奖励追加：
        //   · 巢穴（Hive，第二幕）：CanonicalEncounter 有 25% 概率掷中 BowlbugBoulderEncounter → 掉落；
        //   · 掷空或其它幕：伏击对手是本幕随机精英 → extraRewards 传空，只给标准精英奖励，不掉「巨石」。
        // 与 PragmatistRewardPatch 的判定保持一致（那边负责把精英遗物奖励替换成「巨石」）。
        Player player = Owner!;
        IReadOnlyList<Reward> extraRewards = elite is BowlbugBoulderEncounter
            ?
            [
                new RelicReward(ModelDb.Relic<Boulder>().ToMutable(), player)
            ]
            : [];

        // ⚠ 这里必须传 **canonical**（ModelDb 里的单例）遭遇，不能传 elite.ToMutable()。
        // 实机版（0.110+）的 EventCombatSynchronizer.EnterCombat() 是这样校验的：
        //     EncounterModel canonicalEncounter = _states[0].canonicalEncounter;
        //     for (i...) if (_states[i].canonicalEncounter != canonicalEncounter) throw ...
        // —— 用的是**引用相等**，而消息里只打印 .Id。共享事件在每台机器上「每个玩家各有一份
        // EventModel 克隆」，每份都会调用一次本方法；若各自 ToMutable() 造出新实例，两个引用必然不等，
        // 于是抛 “Event for player … tried to start event combat with encounter X, but the host says
        // it should be X!”（两边 Id 一模一样，只看日志极易误判），异常会打断事件选项任务 → 事件卡死。
        // 单人从来不报，只是因为 _states 只有 1 项、自己和自己比。
        // 原版 0.107 的 EventModel.EnterCombatWithoutExitingEvent<T> 自己传的是 ToMutable()，
        // 实机版已改成传 canonical（见实机反编译：EnterCombatWithoutExitingEvent<T> → ModelDb.Encounter<T>()）；
        // 我们 CanonicalEncounter 取到的本来就是 canonical（Hive 幕的 ModelDb.Encounter<>()，其它幕的
        // _rooms.eliteEncounters[i]，都是 ModelDb 单例），直接传即可。
        // 参考对比：本 mod 的「野生中立伏兵」用的是泛型重载 EnterCombatWithoutExitingEvent<T>()，
        // 它内部就是 ModelDb.Encounter<T>()，所以那条路一直没有这个问题。
        EnterCombatWithoutExitingEvent(elite, extraRewards, shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
