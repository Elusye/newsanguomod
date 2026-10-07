using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using NRun = MegaCrit.Sts2.Core.Nodes.NRun;

namespace newsanguo.Scripts.Patches;

// 「创造模式」的选牌屏要像原版卡牌图鉴那样能搜索 / 筛选（玩家需求）。
//
// 为什么是补丁而不是自造选牌屏：
//   * NSimpleCardSelectScreen 是 sealed，_grid / _cards 是基类 NCardGridSelectionScreen 的
//     protected 字段，没法继承扩展；
//   * 更要紧的是多人同步（ReserveChoiceId / SyncLocalChoice / WaitForRemoteChoice）都写在
//     CardSelectCmd.FromSimpleGridForRewards 里，自造界面会绕过它，联机必然出分歧。
//   所以选牌流程照旧走原版，只在那一屏的 _Ready 之后把图鉴的侧栏挂上去。
//
// 为什么直接复用图鉴场景里的侧栏（而不是自己搭一套）：
//   * 搜索框、类型/稀有度/费用勾选、四种排序按钮都是图鉴里的现成控件，连本地化键都能照抄；
//   * 自己搭布局既要重做图标/主题，又容易和原版的悬停提示、字号规则打架。
//   做法：实例化 card_library.tscn（**不入树，所以它自己的 _Ready 不会跑**）→ 取出 Sidebar →
//   把整棵图鉴场景挂到选牌屏的 %CardGrid 下 → 隐藏用不到的卡池按钮与底部统计勾选。
//   注意：必须让图鉴根节点（而不是单独的侧栏）入树——侧栏内部控件的 "%UniqueName" 是靠
//   owner（即图鉴根节点）解析的，把侧栏单独摘出去会让这些 _Ready 里的 % 查找失败。
//
// 为什么要把卡牌区域右移：
//   NCardGrid.InitGrid/UpdateGridPositions 是按 ScrollContainer 的宽度算列数、再在容器里
//   横向居中的（((容器宽 - 卡牌总宽) * 0.5f)），所以把 ScrollContainer 的左边界加宽一个侧栏
//   宽度即可：卡牌会自动重新居中，不会跑到侧栏底下。
//
// 只在「创造模式」这一屏生效：CreativeModeReward 弹出选牌屏之前打一个一次性标记，
// 补丁消费掉这个标记才装配 UI（原版其它选牌屏弹出的 NSimpleCardSelectScreen 不加搜索）。
internal static class CreativeModePickerSearch
{
    private static bool _pending;

    /// <summary>由「创造模式」奖励在弹出选牌屏之前调用。</summary>
    internal static void Request()
    {
        _pending = true;
    }

    /// <summary>撤销标记（选牌屏根本没弹出来时用，避免污染之后别的选牌屏）。</summary>
    internal static void Clear()
    {
        _pending = false;
    }

    /// <summary>只看标记、不消费（Prefix 要提前内缩卡牌区域，Postfix 还要靠它决定装不装 UI）。</summary>
    internal static bool IsPending => _pending;

    /// <summary>取走标记；同一个标记只会被用掉一次。</summary>
    internal static bool ConsumePending()
    {
        if (!_pending)
        {
            return false;
        }

        _pending = false;
        return true;
    }
}

[HarmonyPatch(typeof(NSimpleCardSelectScreen), "_Ready")]
public static class CardSelectSearchPatch
{
    private static readonly FieldInfo? GridField = AccessTools.Field(typeof(NCardGridSelectionScreen), "_grid");

    private static readonly FieldInfo? CardsField = AccessTools.Field(typeof(NCardGridSelectionScreen), "_cards");

    /// <summary>
    /// 关键顺序：必须赶在原版 _Ready 里 ConnectSignalsAndInitGrid() 排卡牌**之前**把网格内缩掉，
    /// 卡牌才会一开始就按窄一栏的宽度分列。这和原版图鉴场景把 offset_left = 288 直接写进
    /// card_library.tscn 是同一个道理（创意工坊 Loadout 的选牌屏也是场景里就带 offset_left）。
    /// 只放在 Postfix 里改，就只能等网格自己收到 resize 通知再重排，时机不稳。
    /// </summary>
    [HarmonyPrefix]
    private static void InsetCardAreaEarly(NSimpleCardSelectScreen __instance)
    {
        if (!CreativeModePickerSearch.IsPending)
        {
            return;
        }

        try
        {
            if (GridField?.GetValue(__instance) is NCardGrid grid)
            {
                PickerSearchUi.InsetCardArea(grid, PickerSearchUi.FallbackSidebarWidth);
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：提前内缩卡牌区域失败（装配时还会再试一次）：{e.Message}", 1);
        }
    }

    [HarmonyPostfix]
    private static void AddSearchUi(NSimpleCardSelectScreen __instance)
    {
        if (!CreativeModePickerSearch.ConsumePending())
        {
            return;
        }

        try
        {
            if (GridField?.GetValue(__instance) is not NCardGrid grid)
            {
                Entry.Logger.Warn("[创造模式] 选牌屏搜索：拿不到卡牌网格，跳过（不影响选牌）。", 1);
                return;
            }

            if (CardsField?.GetValue(__instance) is not IReadOnlyList<CardModel> cards || cards.Count == 0)
            {
                Entry.Logger.Warn("[创造模式] 选牌屏搜索：拿不到候选牌列表，跳过（不影响选牌）。", 1);
                return;
            }

            PickerSearchUi.Attach(grid, cards);
        }
        catch (Exception e)
        {
            // 搜索只是锦上添花：任何异常都不该让玩家卡在选牌屏
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索 UI 装配失败（不影响选牌）：{e}", 1);
        }
    }
}

/// <summary>
/// 「创造模式」选牌屏上那一列图鉴式侧栏：搜索框 + 类型/稀有度/费用筛选 + 四种排序。
/// 刻意不继承 Godot 节点（只持有节点引用），避免依赖脚本绑定，全部状态都在这个普通对象里。
/// </summary>
internal sealed class PickerSearchUi
{
    // 搜索框输入后等这么久再重排（每个按键都重建上千个卡牌节点会卡）；与实际执行同一线程
    private const double SearchDebounceSeconds = 0.25;

    // 图鉴场景根节点的名字（实例化后会改成这个，查找路径才稳定）
    private const string LibraryRootName = "CardLibrary";

    // 侧栏默认宽度（card_library.tscn 里 Sidebar 的 offset_right）；取不到实际宽度时的兜底
    internal const float FallbackSidebarWidth = 288f;

    // card_grid.tscn 里 %ScrollContainer 相对卡牌网格的左右内缩（卡牌按这个宽度分列并居中）
    private const float CardAreaLeftInset = 150f;
    private const float CardAreaRightInset = -200f;

    // 让出侧栏宽度后补排一次卡牌的延迟：Godot 的布局比 deferred 调用还晚一步
    private const double RelayoutDelaySeconds = 0.1;

    // 自建 CanvasLayer 的名字与层级：把侧栏抬进这一层，游戏自带的战斗 HUD（顶部血条/药水、
    // 左侧遗物列表）都在默认层 0，就再也不可能画到侧栏上面。
    // 这就是「照搬 Loadout」的那一步（`NLoadoutPanelRoot.GetOrCreateOverlayLayer`：在 /root 下
    // 自己开一个 `new CanvasLayer { Layer = 1000 }`，而不是把面板塞进游戏现有的 UI 树）。
    private const string OverlayLayerName = "NewsanguoPickerOverlayLayer";
    private const int OverlayLayerLevel = 1000;

    // card_library.tscn 里侧栏的底色是 `Sidebar/Panel`（ColorRect，color = (0.182, 0.2604, 0.28, 0.501961)）：
    // 原版图鉴那层半透明底色是压在图鉴自己的暗背景上的，搬到选牌屏上就会透出后面的战斗/奖励画面，
    // 搜索框和筛选按钮看着就发灰、不好读。这里把它提到接近不透明（玩家反馈「不透明度可以调高一些」）。
    private const float SidebarPanelAlpha = 0.96f;

    // 卡池按钮里，RitsuLib 给「本 mod 角色」追加的那些叫 MOD_FILTER_{CharacterModel.Id.Entry}
    // （见 Scripts/Patches/CardLibraryPoolContext.cs）；原版那 8 个按钮就是节点名本身。
    private const string ModPoolFilterPrefix = "MOD_FILTER_";

    // 图鉴自带的返回箭头位置偏高（玩家反馈「可以再往下一点」），按这个像素量往下挪。
    // NBackButton 的落点是它自己算的：_Ready 里 `_posOffset = (OffsetLeft + 80, -OffsetBottom + 110)`、
    // `_showPos = (0, 视口高) - _posOffset` ⇒ 落点 y = 视口高 + OffsetBottom - 110，即 OffsetBottom
    // 每 +1 按钮就往下 1px。必须在它 _Ready（＝图鉴入树）之前改。
    private const float BackButtonExtraBottomOffset = 112f;

    private readonly NCardGrid _grid;
    private readonly Node _screen;
    private readonly IReadOnlyList<CardModel> _allCards;
    private readonly IReadOnlyDictionary<string, Node> _nodes;
    private readonly Control _shell;
    private readonly NSearchBar? _searchBar;
    private readonly RichTextLabel? _noResultsLabel;

    // 承载侧栏的 CanvasLayer（这一屏关闭时回收）
    private CanvasLayer? _overlayLayer;

    // NCardGrid.InitGrid()（protected 无参）就是游戏自己在 resize 通知里调来重排卡牌的方法，
    // 这里也直接调一次：光靠 SetCards 不一定在布局结算之后跑，卡牌就会按旧宽度分列、最右列出屏。
    private static readonly MethodInfo? InitGridMethod = AccessTools.Method(typeof(NCardGrid), "InitGrid", Type.EmptyTypes);

    // NSimpleCardSelectScreen.CompleteSelection()（private 无参）= 以当前已选（可为空）完成本次选牌，
    // 并把这一屏从 overlay 栈里移除（STS2 源码：`_completionSource.SetResult(_selectedCards); NOverlayStack.Instance.Remove(this);`）。
    // 右上角叉号就调它：一张没选 ⇒ 奖励那边收到空列表、OnSelect 返回 false，奖励留在奖励屏上。
    private static readonly MethodInfo? CompleteSelectionMethod = AccessTools.Method(typeof(NSimpleCardSelectScreen), "CompleteSelection", Type.EmptyTypes);

    private readonly List<SortingOrders> _sorting = [SortingOrders.Ascending];

    // 每一组筛选内部是“或”，组与组之间是“且”；组内一个都没勾 => 该组不设限
    private readonly List<(Func<CardModel, bool> Matches, Func<bool> IsTicked)> _typeFilters = [];
    private readonly List<(Func<CardModel, bool> Matches, Func<bool> IsTicked)> _rarityFilters = [];
    private readonly List<(Func<CardModel, bool> Matches, Func<bool> IsTicked)> _costFilters = [];

    // 「按角色/卡池分类」：就是图鉴侧栏上那一排卡池按钮，单选；一个都没选 = 不按角色筛选。
    // 注意图鉴自己的 UpdateFilter 在「一个池都没选」时是 poolFilter.Any() 恒假、一张牌都不显示；
    // 选牌屏不能照抄这一点，否则默认状态（没有角色池按钮被选中）会空屏。
    private readonly List<(Func<CardModel, bool> Matches, Func<bool> IsTicked)> _poolFilters = [];
    private readonly List<NCardPoolFilter> _poolButtons = [];

    // 这一屏打开期间被我们临时禁用的游戏 HUD 按钮（右上角设置/牌堆/地图/药水等）。
    // NClickableControl.Disable/Enable 都是带 _isEnabled 守卫的幂等操作，只把「原本可用」的记下来还原即可。
    private readonly List<NClickableControl> _frozenHudControls = [];

    private PickerSearchUi(
        NCardGrid grid,
        IReadOnlyList<CardModel> allCards,
        IReadOnlyDictionary<string, Node> nodes,
        Node screen,
        Control shell,
        CanvasLayer overlayLayer)
    {
        _grid = grid;
        _allCards = allCards;
        _nodes = nodes;
        _screen = screen;
        _shell = shell;
        _overlayLayer = overlayLayer;
        _searchBar = Find<NSearchBar>("SearchBar");
        _noResultsLabel = Find<RichTextLabel>("NoResultsLabel");
    }

    /// <summary>图鉴场景里要用的节点名（场景里都给它们设了 %唯一名，也就是节点名）。</summary>
    private static readonly string[] WantedNames =
    [
        "Sidebar", "SearchBar", "NoResultsLabel", "CardGrid", "CardCountLabel", "BackButton",
        "Spacer2", "PoolFilters", "BottomVBox",
        "IroncladPool", "SilentPool", "DefectPool", "RegentPool", "NecrobinderPool", "ColorlessPool", "AncientsPool", "MiscPool",
        "AttackType", "SkillType", "PowerType", "OtherType",
        "CommonRarity", "UncommonRarity", "RareRarity", "OtherRarity",
        "Cost0", "Cost1", "Cost2", "Cost3+", "CostX",
        "CardTypeSorter", "RaritySorter", "CostSorter", "AlphabetSorter",
        "Stats", "Upgrades", "MultiplayerCards",
    ];

    /// <summary>
    /// 在图鉴场景里按节点名广度搜一遍，把我们需要的节点抓进字典。
    /// 必须在 DetachLibraryScript 之前调用：摘掉根脚本会让根节点的托管包装失效，
    /// 之后再从根节点取任何子节点都会抛 ObjectDisposedException。
    /// 用「按名字搜」而不是 NodePath / %唯一名：这棵子树是摘掉脚本后才塞进选牌屏的，
    /// 不依赖 % 的 owner 解析，也不怕版本换了层级。
    /// </summary>
    private static Dictionary<string, Node> CollectNodes(Node root)
    {
        Dictionary<string, Node> found = [];
        Queue<Node> queue = new();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            foreach (Node child in queue.Dequeue().GetChildren())
            {
                string name = child.Name.ToString();
                if (!found.ContainsKey(name) && Array.IndexOf(WantedNames, name) >= 0)
                {
                    found[name] = child;
                }

                queue.Enqueue(child);
            }
        }

        return found;
    }

    private T? Find<T>(string name)
        where T : Node
    {
        if (_nodes.TryGetValue(name, out Node? node) && node is T typed && GodotObject.IsInstanceValid(typed))
        {
            return typed;
        }

        return null;
    }

    /// <summary>把图鉴侧栏挂到这一屏的卡牌网格上。</summary>
    internal static void Attach(NCardGrid grid, IReadOnlyList<CardModel> cards)
    {
        PackedScene? scene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("screens/card_library/card_library"));
        if (scene is null)
        {
            Entry.Logger.Warn("[创造模式] 选牌屏搜索：加载图鉴场景失败，跳过。", 1);
            return;
        }

        // 图鉴根节点上挂着 NCardLibrary：它的 _Ready 会把整套图鉴初始化起来（建自己的卡网格、
        // 连接自己的筛选回调，还会去改 %NoResultsLabel 的可见性）。我们只用那一列侧栏，所以：
        //   1) 先把图鉴塞进一个「还没进树」的空壳节点 —— 没进树的节点不会跑 _Ready；
        //   2) 再摘掉图鉴根节点的脚本（SetScript 会让原托管对象失效，之后一律只用壳节点访问）；
        //   3) 最后连壳一起挂进自建显示层，侧栏子节点的脚本照常 _Ready。
        Node library = scene.Instantiate();
        library.Name = LibraryRootName;

        // 返回箭头要在它 _Ready 之前挪（_Ready 会把落点算好缓存起来），趁图鉴还没入树先改。
        LowerBackButton(library);

        // 壳做成 Control 并铺满屏幕、挂进下面那个自建 CanvasLayer：侧栏的锚点是相对视口算的
        // （CanvasLayer 不是 Control，Control 的锚点就按视口矩形结算），网格内缩左边界时侧栏原地不动。
        // 如果壳是普通 Node，Godot 会把图鉴根节点的锚点按视口算；挂进网格又会跟着网格跑，
        // 两种都试过，都会让侧栏跑到屏幕外。
        // MouseFilter=Ignore：整块 Control 默认会吃掉鼠标事件，把底下卡牌的点击也挡住；
        // 只影响它自己，侧栏里的子控件照常接收输入。
        Control shell = new() { Name = "NewsanguoPickerSearch", MouseFilter = Control.MouseFilterEnum.Ignore };
        shell.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        shell.AddChild(library, forceReadableName: false, Node.InternalMode.Disabled);

        // 同样要 Ignore：图鉴根也是铺满全屏的 Control，默认 MouseFilter=Stop 会把整屏的
        // 鼠标事件全吃掉 —— 卡牌就点不动了（实测踩过）。子控件照常接收输入。
        if (library is Control libraryControl)
        {
            libraryControl.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        // 要用的节点必须趁根节点的托管包装还有效时抓：SetScript 之后它就不能再取子节点了
        Dictionary<string, Node> nodes = CollectNodes(library);

        DetachLibraryScript(library);

        // 摘掉根脚本之后才让整棵图鉴入树：这样 NCardLibrary._Ready 不会跑（它会建自己的卡网格、
        // 接自己的筛选回调、还会去改 %NoResultsLabel 的可见性），而侧栏子节点的脚本照常 _Ready。
        // 后续的标签初始化依赖子节点已经 _Ready，所以入树必须在这一行之前。
        Node? host = grid.GetParent();
        if (host is null)
        {
            Entry.Logger.Warn("[创造模式] 选牌屏搜索：卡牌网格没有父节点，跳过。", 1);
            shell.QueueFree();
            return;
        }

        // 整块侧栏（连同图鉴整棵子树）挂进 /root 下的自建 CanvasLayer(Layer=1000)：它在所有默认层 0
        // 的游戏 UI 之上，侧栏最上面那个搜索框就不会再被战斗 HUD 的遗物列表/血条盖住（用户实测的问题）。
        // 只抬侧栏、**不抬整屏**：NCardGridSelectionScreen._ExitTree() 里有
        // `_completionSource.TrySetCanceled()`，把选牌屏移出场景树再放回去会让 CardsSelected() 那边的
        // await 抛 TaskCanceledException，整条奖励流程就断了。
        CanvasLayer? overlayLayer = AttachToOverlayLayer(shell, host);
        if (overlayLayer is null)
        {
            return;
        }

        if (!nodes.TryGetValue("Sidebar", out Node? sidebarNode) || sidebarNode is not Control)
        {
            Entry.Logger.Warn("[创造模式] 选牌屏搜索：图鉴场景里没有 Sidebar，跳过。", 1);
            overlayLayer.QueueFree();
            return;
        }

        // 图鉴自己的卡网格用不到（我们复用原选牌屏的网格），直接隐藏，免得它跟着一起显示。
        // BackButton 反而**不能**隐藏：图鉴场景自带的那颗左下角返回箭头就是我们的「返回奖励界面」。
        Hide(nodes, "CardGrid");
        Hide(nodes, "CardCountLabel");

        // 留搜索 + 「按角色/卡池分类」那一排卡池按钮 + 三种筛选 + 四种排序；只隐藏选牌场景里没有意义的
        // 底部统计勾选（多人牌 / 卡牌数据 / 升级预览）。卡池按钮**不再隐藏**：玩家要的就是图鉴那套按角色分类
        // （它们由 WirePools() 接成单选筛选；RitsuLib 注入的 MOD_FILTER_* 也在那一排里）。
        foreach (string name in new[]
        {
            "Spacer2", "BottomVBox",
            "Stats", "Upgrades", "MultiplayerCards",
        })
        {
            Hide(nodes, name);
        }

        PickerSearchUi ui = new(grid, cards, nodes, host, shell, overlayLayer);

        ui.LocalizeNoResults();
        ui.Wire();
        ui.ShiftCardAreaRight();

        // 侧栏底色几乎全不透明（图鉴原值是 0.5，压在选牌屏上会透出后面的画面）
        ui.BoostSidebarOpacity();

        // 图鉴那一排卡池按钮 = 「按角色分类」
        ui.WirePools();

        // 左下角那顆返回箭头（图鉴场景自带）：启用它 + 接上「不选牌，直接回到奖励界面」。
        ui.WireBackButton();

        // 这一屏开着的时候，右上角那排游戏按钮（设置/牌堆/地图/药水…）要失效，否则可能点出别的界面
        ui.FreezeGameHud();

        // 这一屏关闭时（点返回箭头 / 选中卡牌自动完成 / 整局退出）把自建显示层连同侧栏一起回收。
        host.Connect(Node.SignalName.TreeExiting, Callable.From(ui.Cleanup));

        // 网格的最终尺寸要等这一帧布局跑完才算准（列数与居中都按 ScrollContainer 的宽度算），
        // 所以首帧末先按新宽度排一次。
        Callable.From(ui.Apply).CallDeferred();

        // 布局在 Godot 里比 deferred 调用还晚一步，而且 NCardGrid 只在收到自己的 resize 通知时
        // 才会重排（_needsReinit → InitGrid），这一屏不一定触发；再补一次短延迟重排，
        // 确保卡牌是按内缩后的宽度算的 —— 否则卡牌会连着容器一起右移、最右一列出屏。
        SceneTree? tree = grid.GetTree();
        if (tree is not null)
        {
            tree.CreateTimer(RelayoutDelaySeconds).Timeout += () =>
            {
                // 这时网格尺寸已经结算完，先把容器宽度按新宽度重新钉死，再让网格按它重排列数。
                ui.ShiftCardAreaRight();
                ui.Apply();
                ui.ForceGridRelayout();
                ui.LogGeometry();
            };
        }

        Entry.Logger.Info($"[创造模式] 选牌屏已挂上图鉴式搜索/筛选侧栏（候选 {cards.Count} 张）。");
    }

    private static void DetachLibraryScript(Node library)
    {
        try
        {
            if (library.GetScript().VariantType != Variant.Type.Nil)
            {
                library.SetScript(default(Variant));
            }
        }
        catch (Exception e)
        {
            // 摘不掉也能用：只是图鉴会多初始化一份自己的卡网格，并且它的筛选回调会跟着跑一遍
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：摘除图鉴根脚本失败：{e.Message}", 1);
        }
    }

    private static void Hide(IReadOnlyDictionary<string, Node> nodes, string name)
    {
        if (nodes.TryGetValue(name, out Node? node) && node is CanvasItem item)
        {
            item.Visible = false;
            return;
        }

        Entry.Logger.Warn($"[创造模式] 选牌屏搜索：图鉴场景里找不到要隐藏的节点 {name}（忽略）。", 1);
    }

    // 无结果提示的文案来自图鉴场景（英文 "No cards found."），换成 mod 的本地化键；
    // 键还没随 pck 一起导出时会被回落成键名本身，那就保留场景里的英文兜底。
    private void LocalizeNoResults()
    {
        if (_noResultsLabel is null)
        {
            return;
        }

        const string key = "NEWSANGUO_CARD_CREATIVE_MODE_NO_RESULTS";
        try
        {
            string text = new LocString("cards", key).GetRawText();
            if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, key, StringComparison.Ordinal))
            {
                _noResultsLabel.Text = text;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：无结果提示本地化失败（保留英文兜底）：{e.Message}", 1);
        }
    }

    /// <summary>
    /// 给侧栏让出左侧一条宽度：做法和图鉴场景里的 CardGrid（offset_left = 288）完全一样。
    /// 动卡牌网格根节点还有个关键作用：它尺寸一变就会收到 resize 通知（_needsReinit → InitGrid），
    /// 卡牌才会按新宽度重新分列、重新居中。只挪它内部的 ScrollContainer 不触发重排，
    /// 卡牌会连着容器整块右移、最右边一列跑出屏幕（实测就是这个现象）。
    /// </summary>
    private void ShiftCardAreaRight()
    {
        float width = Find<Control>("Sidebar") is { } sidebar ? sidebar.OffsetRight - sidebar.OffsetLeft : 0f;
        if (width <= 0f)
        {
            width = FallbackSidebarWidth;
        }

        InsetCardArea(_grid, width);
    }

    /// <summary>
    /// 给侧栏让出左侧一条宽度：做法和图鉴场景里的 CardGrid（offset_left = 288）完全一样。
    /// 动卡牌网格根节点还有个关键作用：它尺寸一变就会收到 resize 通知（_needsReinit → InitGrid），
    /// 卡牌才会按新宽度重新分列、重新居中。只挪它内部的 ScrollContainer 不触发重排，
    /// 卡牌会连着容器整块右移、最右边一列跑出屏幕（实测就是这个现象）。
    /// 注意：最要紧的是让它在原版 _Ready 排卡牌**之前**就生效（见 InsetCardAreaEarly），
    /// 否则只能等网格自己收到 resize 通知再重排，时机不稳。
    /// </summary>
    internal static void InsetCardArea(NCardGrid grid, float width)
    {
        if (!GodotObject.IsInstanceValid(grid))
        {
            return;
        }

        grid.AnchorLeft = 0f;
        grid.AnchorRight = 1f;
        grid.OffsetLeft = width;

        Control? scroll = grid.GetNodeOrNull<Control>("%ScrollContainer");
        if (scroll is null)
        {
            return;
        }

        // 内缩后的网格宽度。布局还没结算时 grid.Size 还是内缩前的整屏宽（等于父节点宽），
        // 这时用「父节点宽 - 侧栏宽」兜底。
        float parentWidth = (grid.GetParent() as Control)?.Size.X ?? 0f;
        float gridWidth = grid.Size.X;
        if (gridWidth <= 0f || gridWidth >= parentWidth)
        {
            gridWidth = parentWidth - width;
        }

        // 网格内缩后：容器 = 左边距 150 → 网格右边界前 200，即宽 = 网格宽 - 350。
        // 网格 1440 宽 ⇒ 容器 1090 宽 ⇒ (1090+40)/(240+40) = 4 列、块宽 1080，正好放得下。
        float cardAreaWidth = gridWidth - CardAreaLeftInset + CardAreaRightInset;
        if (cardAreaWidth <= 0f)
        {
            return;
        }

        // 关键：把容器从「跟着父节点宽度自动伸缩」解耦 —— 左右锚点都钉在 0，宽度只由偏移量决定。
        // 原先左右锚点是 0/1（按父节点宽度伸缩），但 Godot 结算锚点时读到的父节点矩形往往还是
        // 内缩前的 1728 ⇒ 容器宽 1378、右边界 1816 跑到屏幕外，卡牌就按 1378 排 5 列、最右一列出屏
        // （实测日志：`滚动容器 pos=(438,80) size=(1378,62286.402)`、`卡牌 25 张 x=567..1687`）。
        // 锚点钉成 0 之后宽度不再由父节点决定，之后任何一次布局结算都不会再把它改回去。
        scroll.AnchorLeft = 0f;
        scroll.AnchorRight = 0f;
        scroll.OffsetLeft = CardAreaLeftInset;
        scroll.OffsetRight = CardAreaLeftInset + cardAreaWidth;
        scroll.Size = new Vector2(cardAreaWidth, scroll.Size.Y);
    }

    /// <summary>强制卡牌网格按当前宽度重排一次（游戏自己在 resize 通知里调的就是这个方法）。</summary>
    internal void ForceGridRelayout()
    {
        try
        {
            if (!GodotObject.IsInstanceValid(_grid))
            {
                return;
            }

            InitGridMethod?.Invoke(_grid, null);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：强制卡牌网格重排失败：{e.Message}", 1);
        }
    }

    /// <summary>
    /// 战斗 HUD（屏幕上方血条/药水位 NTopBar、左侧遗物列表 NRelicInventory）和选牌屏都在默认画布层 0，
    /// 绘制顺序就是树序 —— HUD 是后面的兄弟，所以画在侧栏之上、盖住最上面那个搜索框（用户实测）。
    /// 实测「量出 HUD 底边、让侧栏内容往下躲」不可行（战斗里遗物列表很高，搜索框会被顶到屏幕最底下），
    /// 所以改成照搬 Loadout 的做法：自己开一个高图层，把侧栏整块抬上去。
    /// </summary>
    private static CanvasLayer? AttachToOverlayLayer(Control shell, Node context)
    {
        try
        {
            SceneTree? tree = context.GetTree();
            if (tree is null)
            {
                Entry.Logger.Warn("[创造模式] 选牌屏搜索：拿不到场景树，跳过。", 1);
                return null;
            }

            CanvasLayer layer;
            if (tree.Root.GetNodeOrNull<CanvasLayer>(OverlayLayerName) is { } existing)
            {
                // 上一屏万一没清干净，复用同一层，先把里面残留的东西回收掉
                layer = existing;
                layer.Layer = OverlayLayerLevel;
                foreach (Node leftover in layer.GetChildren())
                {
                    leftover.QueueFree();
                }
            }
            else
            {
                layer = new CanvasLayer { Name = OverlayLayerName, Layer = OverlayLayerLevel };
                tree.Root.AddChild(layer, forceReadableName: false, Node.InternalMode.Disabled);
            }

            layer.AddChild(shell, forceReadableName: false, Node.InternalMode.Disabled);
            return layer;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：挂自建显示层失败（侧栏可能被战斗 HUD 挡住）：{e.Message}", 1);
            return null;
        }
    }

    /// <summary>
    /// 把图鉴自带的返回箭头往下挪一点。`NBackButton._Ready` 里会把它自己的落点算成
    /// `_showPos = (0, 视口高) - (OffsetLeft + 80, -OffsetBottom + 110)`（`OnWindowChange` 同理，
    /// 窗口尺寸变化时也按缓存的 `_posOffset` 重算），所以这里改 OffsetTop/OffsetBottom（一起改、
    /// 高度不变）就等于整体下移。**必须赶在它 _Ready 之前**：`_posOffset` 只在 `_Ready` 里算一次，
    /// 之后改 Offset 只动布局矩形、按钮位置还是旧值。
    /// </summary>
    private static void LowerBackButton(Node library)
    {
        try
        {
            if (library.GetNodeOrNull<NBackButton>("BackButton") is not { } back)
            {
                Diagnostics.Log("[创造模式] 图鉴场景里没找到返回箭头，位置没挪（不影响它能不能用）。");
                return;
            }

            back.OffsetTop += BackButtonExtraBottomOffset;
            back.OffsetBottom += BackButtonExtraBottomOffset;
            Diagnostics.Log($"[创造模式] 返回箭头下移 {BackButtonExtraBottomOffset:0}px：OffsetTop={back.OffsetTop:0}、OffsetBottom={back.OffsetBottom:0}");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：挪返回箭头位置失败（不影响它能不能用）：{e.Message}", 1);
        }
    }

    /// <summary>
    /// 用图鉴场景自带的左下角返回箭头（`NSubmenu._Ready` 里的 `GetNode<NBackButton>("BackButton")`）
    /// 当「返回奖励界面」。它本身就是游戏标准的返回按钮：位置、动画、音效都由 NBackButton 自己算
    /// （`_Ready` 里最后一句是 `OnDisable()`，会把按钮藏到屏幕外，必须 `Enable()` 才会滑进来）。
    /// 图鉴的根脚本被我们摘掉了，所以没人替它接信号 —— 这里补上，点它 = 一张都不选、结束这一屏。
    /// </summary>
    internal void WireBackButton()
    {
        try
        {
            if (Find<NBackButton>("BackButton") is not { } back)
            {
                Entry.Logger.Warn("[创造模式] 选牌屏搜索：图鉴场景里没有返回按钮（用原版方式结束选牌即可）。", 1);
                return;
            }

            back.Visible = true;
            back.TooltipText = CloseTooltipText();
            back.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => ClosePicker()));
            back.Enable();
            Entry.Logger.Info("[创造模式] 选牌屏：左下角返回箭头已启用（点它＝不选牌，回到奖励界面）。");

            // NBackButton 的落点是 _showPos = (0, 视口高) - (OffsetLeft + 80, -OffsetBottom + 110)，
            // 记一笔实际值，位置不对时好对着算出该再挪多少。
            Diagnostics.Log($"[创造模式] 返回箭头：OffsetBottom={back.OffsetBottom:0}、视口高={back.GetViewportRect().Size.Y:0} ⇒ 落点 y≈{back.GetViewportRect().Size.Y + back.OffsetBottom - 110f:0}");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：启用左下角返回箭头失败（用原版方式结束选牌即可）：{e.Message}", 1);
        }
    }

    /// <summary>
    /// 提高侧栏底色的不透明度。图鉴场景里 `Sidebar/Panel` 是个 ColorRect，颜色 alpha 只有 0.501961
    /// （图鉴里它压在图鉴自己的暗背景上，半透明刚好）。选牌屏背后是战斗/奖励画面，这层 0.5 的底色
    /// 就会透过去，搜索框和筛选按钮看着发灰、不好读 —— 玩家反馈「不透明度可以调高一些」。
    /// </summary>
    internal void BoostSidebarOpacity()
    {
        try
        {
            if (Find<Control>("Sidebar") is not { } sidebar)
            {
                WarnMissing("Sidebar");
                return;
            }

            // 用直接子节点路径取 Panel：`Panel` 这种通用名在整棵图鉴里不止一个，不能走 _nodes 字典
            if (sidebar.GetNodeOrNull<ColorRect>("Panel") is not { } panel)
            {
                Entry.Logger.Warn("[创造模式] 选牌屏搜索：侧栏里没找到底色 Panel，不透明度没调。", 1);
                return;
            }

            Color color = panel.Color;
            panel.Color = new Color(color.R, color.G, color.B, SidebarPanelAlpha);
            Diagnostics.Log($"[创造模式] 侧栏底色不透明度 {color.A:0.###} -> {SidebarPanelAlpha:0.###}（{panel.GetPath()}）");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：调侧栏不透明度失败：{e.Message}", 1);
        }
    }

    /// <summary>
    /// 「按角色/卡池分类」：图鉴侧栏上那一排卡池按钮（原版 8 个 + 本 mod 角色的）。
    /// 语义照图鉴：**单选**（选一个就把别的取消）；但一个都没选时当作不设限 ——
    /// 图鉴自己的 UpdateFilter 在没选池时 `poolFilter.Any()` 恒假、一张牌都不显示，选牌屏照抄会空屏。
    /// </summary>
    internal void WirePools()
    {
        try
        {
            if (Find<Control>("PoolFilters") is not { } container)
            {
                WarnMissing("PoolFilters");
                return;
            }

            List<NCardPoolFilter> buttons = [];
            HashSet<CardPoolModel> coveredPools = [];

            foreach (string name in VanillaPoolFilterNames)
            {
                if (Find<NCardPoolFilter>(name) is not { } vanilla || buttons.Contains(vanilla))
                {
                    continue;
                }

                buttons.Add(vanilla);
                if (ResolvePool(name) is { } pool)
                {
                    coveredPools.Add(pool);
                }
            }

            // RitsuLib 注入的按钮名带 MOD_FILTER_ 前缀、不在 WantedNames 名单里，直接从容器里捡；
            // 顺便记下它已经覆盖的池，免得下面又给同一个角色补一颗重复的
            foreach (Node child in container.GetChildren())
            {
                if (child is not NCardPoolFilter extra || buttons.Contains(extra))
                {
                    continue;
                }

                buttons.Add(extra);
                if (ResolvePool(extra.Name.ToString()) is { } covered)
                {
                    coveredPools.Add(covered);
                }
            }

            // 本 mod 角色的池按钮得自己补：RitsuLib 那个「给 mod 角色加池筛选」的 Postfix 挂在
            // NCardLibrary._Ready 上，而我们的实例被摘掉了根脚本，它跑不到这儿来。
            List<NCardPoolFilter> addedForModCharacters = AddModCharacterPoolButtons(container, coveredPools);
            buttons.AddRange(addedForModCharacters);

            foreach (NCardPoolFilter button in buttons)
            {
                if (PoolMatcher(button.Name.ToString()) is not { } matches)
                {
                    // 认不出对应卡池（多为 RitsuLib 新加的按钮、命名变了）就跳过，但记一笔备查
                    Diagnostics.Log($"[创造模式] 卡池按钮 {button.Name} 没认出对应卡池，跳过（点它不会有筛选效果）。");
                    continue;
                }

                _poolButtons.Add(button);
                _poolFilters.Add((matches, () => button.IsSelected));
                button.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(OnPoolFilterToggled));
            }

            // 按钮多了就把列数放宽，别让这一排竖着长出去（图鉴里这一条最多占 3 行 192px）
            ApplyPoolGridColumns(container, buttons.Count);

            Diagnostics.Log($"[创造模式] 卡池按钮：一共 {buttons.Count} 个（原版 {VanillaPoolFilterNames.Length} 个 + 本 mod 角色补 {addedForModCharacters.Count} 个），接上筛选 {_poolFilters.Count} 个。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：接「按角色分类」失败：{e.Message}", 1);
        }
    }

    /// <summary>
    /// 给本 mod 的角色补卡池按钮：实例化原版的 `library_pool_toggle.tscn`（这样外观、着色器、
    /// 选中框都和原版那排一致，`NCardPoolFilter` 的 `_Ready` 也能正常跑到），改名成
    /// `MOD_FILTER_{角色 Id}`、换成该角色的图标，再塞进 `PoolFilters`。
    /// </summary>
    private List<NCardPoolFilter> AddModCharacterPoolButtons(Control container, HashSet<CardPoolModel> coveredPools)
    {
        List<NCardPoolFilter> added = [];

        try
        {
            PackedScene? toggleScene = PreloadManager.Cache.GetScene(
                SceneHelper.GetScenePath("screens/card_library/library_pool_toggle"));
            if (toggleScene is null)
            {
                Diagnostics.Log("[创造模式] 拿不到 library_pool_toggle 场景，按角色分类只能显示原版那几个池。");
                return added;
            }

            foreach (CharacterModel character in ModelDb.AllCharacters)
            {
                CardPoolModel pool;
                try
                {
                    pool = character.CardPool;
                }
                catch (Exception e)
                {
                    Diagnostics.Log($"[创造模式] 角色 {character.Id.Entry} 取卡池失败，跳过：{e.Message}");
                    continue;
                }

                if (pool is null || coveredPools.Contains(pool))
                {
                    continue;
                }

                // 认不出池、或这个池已经有按钮了（原版或别处补过）就不重复加
                coveredPools.Add(pool);

                if (toggleScene.Instantiate() is not NCardPoolFilter button)
                {
                    continue;
                }

                button.Name = ModPoolFilterPrefix + character.Id.Entry;
                ApplyPoolButtonIcon(button, character);
                container.AddChild(button, forceReadableName: false, Node.InternalMode.Disabled);
                added.Add(button);
            }

            if (added.Count > 0)
            {
                Diagnostics.Log($"[创造模式] 已为本 mod 角色补 {added.Count} 个卡池按钮：{string.Join("、", added.Select(b => b.Name.ToString()))}");
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：补本 mod 角色卡池按钮失败：{e.Message}", 1);
        }

        return added;
    }

    /// <summary>把角色图标铺到卡池按钮的 `Image` 与其影子 `Shadow` 上（照 RitsuLib 的做法）。</summary>
    private static void ApplyPoolButtonIcon(NCardPoolFilter button, CharacterModel character)
    {
        try
        {
            if (character.IconTexture is not { } icon)
            {
                // 没有图标就是一颗空白按钮：功能还在，只是认不出是谁，记一笔备查
                Diagnostics.Log($"[创造模式] 角色 {character.Id.Entry} 没有 IconTexture，它的卡池按钮会是空白的。");
                return;
            }

            if (button.GetNodeOrNull<TextureRect>("Image") is not { } image)
            {
                Diagnostics.Log($"[创造模式] 角色 {character.Id.Entry} 的卡池按钮上没有 Image 节点，图标没铺上去。");
                return;
            }

            image.Texture = icon;
            if (image.GetNodeOrNull<TextureRect>("Shadow") is { } shadow)
            {
                shadow.Texture = icon;
            }
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[创造模式] 角色 {character.Id.Entry} 卡池按钮图标没换上：{e.Message}");
        }
    }

    /// <summary>
    /// 这一排按钮的列数：原版 4 列（8 个 = 2 行）；补了角色按钮后按「最多 3 行（192px）」放宽列数，
    /// 免得把下面的筛选/排序挤出屏幕（图鉴里 RitsuLib 也是这么处理的）。
    /// </summary>
    private static void ApplyPoolGridColumns(Control container, int count)
    {
        if (container is not GridContainer grid || count <= 0)
        {
            return;
        }

        int columns = Math.Clamp(Mathf.CeilToInt(count / 3f), 4, 6);
        grid.Columns = columns;
    }

    private static readonly string[] VanillaPoolFilterNames =
    [
        "IroncladPool", "SilentPool", "RegentPool", "NecrobinderPool", "DefectPool",
        "ColorlessPool", "AncientsPool", "MiscPool",
    ];

    /// <summary>
    /// 把一个卡池按钮的名字翻成对应的卡池实例（原版名去 "Pool"、mod 名去 "MOD_FILTER_"）：
    /// 先按角色 Id 找（IroncladPool → Ironclad 角色 → 它的 CardPool），
    /// 找不到角色再按池的类型名兜底（ColorlessPool → ColorlessCardPool，无色池没有对应角色）；
    /// 认不出返回 null。
    /// </summary>
    private static CardPoolModel? ResolvePool(string nodeName)
    {
        // 图鉴里这两颗其实不是「池」，是稀有度：古卡 / 其它（枚举 6..10）
        if (string.Equals(nodeName, "AncientsPool", StringComparison.Ordinal)
            || string.Equals(nodeName, "MiscPool", StringComparison.Ordinal))
        {
            return null;
        }

        // 原版按钮 IroncladPool -> 角色实体的 Id.Entry（"Ironclad"）；RitsuLib 的 MOD_FILTER_{Entry} 本来就是 Entry
        string wanted = nodeName.StartsWith(ModPoolFilterPrefix, StringComparison.Ordinal)
            ? nodeName[ModPoolFilterPrefix.Length..]
            : nodeName.EndsWith("Pool", StringComparison.Ordinal) ? nodeName[..^4] : nodeName;

        if (ModelDb.AllCharacters
                .FirstOrDefault(character => string.Equals(character.Id.Entry, wanted, StringComparison.OrdinalIgnoreCase))
                ?.CardPool is { } characterPool)
        {
            return characterPool;
        }

        // 兜底：按池的类型名匹配（ColorlessPool → ColorlessCardPool）。
        // 只比类型名字符串、不直接引用类型，所以原版那些 internal 的池类型也能命中。
        string poolTypeName = wanted + "CardPool";
        return ModelDb.AllCardPools
            .FirstOrDefault(pool => string.Equals(pool.GetType().Name, poolTypeName, StringComparison.Ordinal));
    }

    /// <summary>把一个卡池按钮的名字翻成「这张牌属不属于该池」的判定；认不出来返回 null。</summary>
    private static Func<CardModel, bool>? PoolMatcher(string nodeName)
    {
        // 图鉴里这两颗其实不是「池」，是稀有度：古卡 / 其它（枚举 6..10）
        if (string.Equals(nodeName, "AncientsPool", StringComparison.Ordinal))
        {
            return card => card.Rarity == CardRarity.Ancient;
        }

        if (string.Equals(nodeName, "MiscPool", StringComparison.Ordinal))
        {
            return card => (uint)(card.Rarity - 6) <= 4u;
        }

        if (ResolvePool(nodeName) is not { } pool)
        {
            return null;
        }

        return card => IsInPool(card, pool);
    }

    private static bool IsInPool(CardModel card, CardPoolModel pool)
    {
        try
        {
            // CardModel.Pool = 「第一个包含这张牌的池」，和原版图鉴的判定一致
            // （不在任何池里的牌它会抛 InvalidProgramException，所以必须包住）。
            // 原版图鉴用的是类型判断（`c.Pool is IroncladCardPool`），这里两者都认：
            // 同一个池在 ModelDb 里是单例，引用相等通常成立；保险起见再加一层类型比较。
            CardPoolModel? cardPool = card.Pool;
            return ReferenceEquals(cardPool, pool) || (cardPool is not null && cardPool.GetType() == pool.GetType());
        }
        catch (Exception)
        {
            return false;
        }
    }

    // 图鉴是单选：选中一颗就把别的都取消（`NCardPoolFilter.IsSelected` 的 setter 只改外观，不会再发 Toggled）。
    // 再点一次同一颗 = 全部取消 = 不按角色筛选 —— 这一点是故意比图鉴宽松：默认状态必须能看到全部候选牌。
    private void OnPoolFilterToggled(NCardPoolFilter filter)
    {
        try
        {
            if (filter.IsSelected)
            {
                foreach (NCardPoolFilter other in _poolButtons)
                {
                    if (!ReferenceEquals(other, filter))
                    {
                        other.IsSelected = false;
                    }
                }
            }

            Apply();
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：卡池筛选出错：{e.Message}", 1);
        }
    }

    /// <summary>
    /// 这一屏打开期间把顶部栏（右上角设置/牌堆/地图/药水/角色头像…）里的按钮临时禁用：
    /// 它们在默认层、位置又在侧栏旁边，点得到，点开别的界面会踩到选牌流程（玩家反馈「不然可能会出问题」）。
    /// 只动 NTopBar 子树：选牌屏自己的卡牌和侧栏都不在里面。离开这一屏时由 ThawGameHud() 原样恢复。
    /// </summary>
    internal void FreezeGameHud()
    {
        try
        {
            if (NRun.Instance?.GlobalUi?.TopBar is not { } topBar)
            {
                Entry.Logger.Warn("[创造模式] 选牌屏搜索：拿不到顶部栏，右上角按钮不会失效。", 1);
                return;
            }

            FreezeControls(topBar);
            Diagnostics.Log($"[创造模式] 选牌屏：已临时禁用 {_frozenHudControls.Count} 个顶部栏按钮（关闭这一屏时恢复）。");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：禁用顶部栏按钮失败：{e.Message}", 1);
        }
    }

    private void FreezeControls(Node root)
    {
        // ActiveScreenProxy 是顶栏用来接管「当前屏幕」焦点的代理控件，禁用它会打断焦点恢复，跳过整棵子树
        if (root.Name.ToString().Contains("Proxy", StringComparison.Ordinal))
        {
            return;
        }

        if (root is NClickableControl control && control.IsEnabled)
        {
            control.Disable();
            _frozenHudControls.Add(control);
        }

        foreach (Node child in root.GetChildren())
        {
            FreezeControls(child);
        }
    }

    /// <summary>把 FreezeGameHud() 禁掉的按钮逐个恢复（只恢复我们确实关掉过的那几个）。</summary>
    internal void ThawGameHud()
    {
        try
        {
            foreach (NClickableControl control in _frozenHudControls)
            {
                if (GodotObject.IsInstanceValid(control))
                {
                    control.Enable();
                }
            }

            if (_frozenHudControls.Count > 0)
            {
                Diagnostics.Log($"[创造模式] 选牌屏：已恢复 {_frozenHudControls.Count} 个顶部栏按钮。");
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：恢复顶部栏按钮失败：{e.Message}", 1);
        }
        finally
        {
            _frozenHudControls.Clear();
        }
    }

    /// <summary>这一屏离开场景树时把自建的 CanvasLayer 连同里面的侧栏一起回收（子节点跟着释放）。</summary>
    internal void Cleanup()
    {
        try
        {
            // 先还原被我们禁用的游戏按钮，再拆侧栏
            ThawGameHud();

            if (GodotObject.IsInstanceValid(_overlayLayer))
            {
                _overlayLayer.QueueFree();
            }

            _overlayLayer = null;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：回收自建显示层失败：{e.Message}", 1);
        }
    }

    /// <summary>返回箭头：以当前已选（通常一张都没有）完成这一屏，回到奖励选择界面。</summary>
    private void ClosePicker()
    {
        try
        {
            if (CompleteSelectionMethod is not null && GodotObject.IsInstanceValid(_screen))
            {
                CompleteSelectionMethod.Invoke(_screen, null);
                return;
            }

            _screen.Call("CompleteSelection");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：返回箭头结束选牌失败：{e.Message}", 1);
        }
    }

    // 返回箭头的悬停提示文案；键还没随 pck 一起导出时会回落成键名本身，那就用英文兜底。
    private static string CloseTooltipText()
    {
        const string key = "NEWSANGUO_CARD_CREATIVE_MODE_CANCEL";
        try
        {
            string text = new LocString("cards", key).GetRawText();
            if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, key, StringComparison.Ordinal))
            {
                return text;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：叉号提示本地化失败（用英文兜底）：{e.Message}", 1);
        }

        return "Back";
    }

    /// <summary>排完一次后把关键矩形记进日志：布局再出问题时有数字可查，不用靠描述猜。</summary>
    internal void LogGeometry()
    {
        try
        {
            Control? sidebar = Find<Control>("Sidebar");
            // 图鉴根我们从壳里按位置取（不能重用 library 那个托管包装：SetScript 之后它就失效了）
            Control? libraryRoot = _shell.GetChildOrNull<Control>(0);
            Control? scroll = _grid.GetNodeOrNull<Control>("%ScrollContainer");
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            int holders = 0;
            Vector2 cardSize = Vector2.Zero;
            foreach (NGridCardHolder holder in _grid.CurrentlyDisplayedCardHolders)
            {
                holders++;
                cardSize = holder.Size;
                float left = holder.GlobalPosition.X;
                minX = Math.Min(minX, left);
                maxX = Math.Max(maxX, left + holder.Size.X * holder.Scale.X);
            }

            // 卡牌块是「按 ScrollContainer 的宽度算列数、再在容器里居中」的，所以真正要看的
            // 是容器的宽度与卡牌实际的 X 范围（列数按旧宽度算的话，块会超出容器右边界被剪掉）。
            int expectedColumns = 0;
            if (scroll is not null && cardSize.X > 0f)
            {
                expectedColumns = Mathf.Max(1, (int)((scroll.Size.X + 40f) / (cardSize.X + 40f)));
            }

            Entry.Logger.Info(
                $"[创造模式] 选牌屏布局：网格 pos={_grid.GlobalPosition} size={_grid.Size} offsetLeft={_grid.OffsetLeft}"
                + $" | 侧栏 pos={sidebar?.GlobalPosition.ToString() ?? "无"} size={sidebar?.Size.ToString() ?? "无"} 可见={sidebar?.IsVisibleInTree().ToString() ?? "无"}"
                + $" | 图鉴根 pos={libraryRoot?.GlobalPosition.ToString() ?? "无"} size={libraryRoot?.Size.ToString() ?? "无"}"
                + $" | 滚动容器 pos={scroll?.GlobalPosition.ToString() ?? "无"} size={scroll?.Size.ToString() ?? "无"}"
                + $" | 卡牌 {holders} 张 卡尺寸={cardSize} 该宽度下应为 {expectedColumns} 列 x={((holders > 0) ? $"{minX:0.#}..{maxX:0.#}" : "无")}");
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏布局诊断日志失败：{e.Message}", 1);
        }
    }

    private void Wire()
    {
        if (_searchBar is not null)
        {
            _searchBar.Connect(NSearchBar.SignalName.QueryChanged, Callable.From<string>(OnQueryChanged));
        }

        // 控件都按节点名从抓好的字典里取（与在侧栏里的层级无关）
        // 类型（纯图标按钮，自带 Toggled 信号）
        AddType("AttackType", "TYPE_ATTACK_TIP", c => c.Type == CardType.Attack);
        AddType("SkillType", "TYPE_SKILL_TIP", c => c.Type == CardType.Skill);
        AddType("PowerType", "TYPE_POWER_TIP", c => c.Type == CardType.Power);
        AddType("OtherType", "TYPE_OTHER_TIP", c => !((uint)(c.Type - 1) <= 2u));

        // 稀有度（带文字标签，Toggled 信号来自 NTickbox 基类）
        AddRarity("CommonRarity", "RARITY_COMMON", "RARITY_COMMON_TIP", c => c.Rarity == CardRarity.Common);
        AddRarity("UncommonRarity", "RARITY_UNCOMMON", "RARITY_UNCOMMON_TIP", c => c.Rarity == CardRarity.Uncommon);
        AddRarity("RareRarity", "RARITY_RARE", "RARITY_RARE_TIP", c => c.Rarity == CardRarity.Rare);
        AddRarity("OtherRarity", "RARITY_OTHER", "RARITY_OTHER_TIP", c => !((uint)(c.Rarity - 2) <= 2u));

        // 费用（图标按钮，且用的是 Released 信号 —— 它没有 Toggled）
        AddCost("Cost0", "COST_ZERO_TIP", c => c.EnergyCost != null && c.EnergyCost.Canonical <= 0 && !c.EnergyCost.CostsX);
        AddCost("Cost1", "COST_ONE_TIP", c => c.EnergyCost.Canonical == 1);
        AddCost("Cost2", "COST_TWO_TIP", c => c.EnergyCost.Canonical == 2);
        AddCost("Cost3+", "COST_THREE_TIP", c => c.EnergyCost.Canonical >= 3);
        AddCost("CostX", "COST_X_TIP", c => c.EnergyCost.CostsX || c.HasStarCostX);

        // 四个排序按钮：同样用 Released，按钮自己在 OnRelease 里已经把 IsDescending 翻好了
        WireSorter("CardTypeSorter", "SORT_TYPE", OnTypeSort);
        WireSorter("RaritySorter", "SORT_RARITY", OnRaritySort);
        WireSorter("CostSorter", "SORT_COST", OnCostSort);
        WireSorter("AlphabetSorter", "SORT_ALPHABET", OnAlphabetSort);
    }

    private void AddType(string name, string tipKey, Func<CardModel, bool> matches)
    {
        if (Find<NCardTypeTickbox>(name) is not { } box)
        {
            WarnMissing(name);
            return;
        }

        SetLoc(box, tipKey, loc => box.Loc = loc);
        _typeFilters.Add((matches, () => box.IsTicked));
        box.Connect(NCardTypeTickbox.SignalName.Toggled, Callable.From<NCardTypeTickbox>(OnTypeFilterToggled));
    }

    private void AddRarity(string name, string labelKey, string tipKey, Func<CardModel, bool> matches)
    {
        if (Find<NCardRarityTickbox>(name) is not { } box)
        {
            WarnMissing(name);
            return;
        }

        SetLabel(box, labelKey);
        SetLoc(box, tipKey, loc => box.Loc = loc);
        _rarityFilters.Add((matches, () => box.IsTicked));
        box.Connect(NTickbox.SignalName.Toggled, Callable.From<NTickbox>(OnRarityFilterToggled));
    }

    private void AddCost(string name, string tipKey, Func<CardModel, bool> matches)
    {
        if (Find<NCardCostTickbox>(name) is not { } box)
        {
            WarnMissing(name);
            return;
        }

        SetLoc(box, tipKey, loc => box.Loc = loc);
        _costFilters.Add((matches, () => box.IsTicked));
        box.Connect(NClickableControl.SignalName.Released, Callable.From<NCardCostTickbox>(OnCostFilterToggled));
    }

    private void WireSorter(string name, string labelKey, Action<NButton> handler)
    {
        if (Find<NCardViewSortButton>(name) is not { } button)
        {
            WarnMissing(name);
            return;
        }

        SetLabel(button, labelKey, "gameplay_ui");
        button.Connect(NClickableControl.SignalName.Released, Callable.From(handler));
    }

    private static void WarnMissing(string name)
    {
        Entry.Logger.Warn($"[创造模式] 选牌屏搜索：图鉴侧栏里缺少控件 {name}（该筛选项不可用）。", 1);
    }

    private static void SetLabel(NCardRarityTickbox box, string key)
    {
        try
        {
            box.SetLabel(new LocString("card_library", key).GetRawText());
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：本地化键 card_library/{key} 读取失败：{e.Message}", 1);
        }
    }

    private static void SetLabel(NCardViewSortButton button, string key, string table)
    {
        try
        {
            button.SetLabel(new LocString(table, key).GetRawText());
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：本地化键 {table}/{key} 读取失败：{e.Message}", 1);
        }
    }

    private static void SetLoc(Node box, string key, Action<LocString> apply)
    {
        try
        {
            // 悬停提示（OnFocus → NHoverTipSet）会直接拿 Loc 构 HoverTip，
            // 所以凡是我们保留显示的勾选控件都必须把 Loc 填上，否则鼠标一悬停就出错。
            apply(new LocString("card_library", key));
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：本地化键 card_library/{key} 读取失败：{e.Message}", 1);
        }
    }

    private void OnQueryChanged(string query)
    {
        // 用 SceneTreeTimer 而不是 Task.Delay：Godot 节点只能在主线程上碰，
        // Task 的续体在线程池上跑，直接用会炸。
        SceneTree? tree = _grid.GetTree();
        if (tree is null)
        {
            return;
        }

        tree.CreateTimer(SearchDebounceSeconds).Timeout += Apply;
    }

    private void OnTypeFilterToggled(NCardTypeTickbox box)
    {
        Apply();
    }

    private void OnRarityFilterToggled(NTickbox box)
    {
        Apply();
    }

    private void OnCostFilterToggled(NCardCostTickbox box)
    {
        Apply();
    }

    private void OnTypeSort(NButton button)
    {
        ChangeSort(SortingOrders.TypeAscending, SortingOrders.TypeDescending, button);
    }

    private void OnRaritySort(NButton button)
    {
        ChangeSort(SortingOrders.RarityAscending, SortingOrders.RarityDescending, button);
    }

    private void OnCostSort(NButton button)
    {
        ChangeSort(SortingOrders.CostAscending, SortingOrders.CostDescending, button);
    }

    private void OnAlphabetSort(NButton button)
    {
        ChangeSort(SortingOrders.AlphabetAscending, SortingOrders.AlphabetDescending, button);
    }

    private void ChangeSort(SortingOrders ascending, SortingOrders descending, NButton button)
    {
        // 按钮在 OnRelease 里已经把自己翻转过了：false = 升序（第一次点就是升序）
        bool isDescending = button is NCardViewSortButton sorter && sorter.IsDescending;
        _sorting.Remove(ascending);
        _sorting.Remove(descending);
        _sorting.Insert(0, isDescending ? descending : ascending);
        Apply();
    }

    /// <summary>按当前勾选与搜索词重排卡牌网格。</summary>
    private void Apply()
    {
        try
        {
            if (!GodotObject.IsInstanceValid(_grid))
            {
                return;
            }

            string query = _searchBar is not null ? NSearchBar.Normalize(_searchBar.Text) : string.Empty;

            List<CardModel> filtered = [];
            foreach (CardModel card in _allCards)
            {
                // 组内「或」、组间「且」；空组 = 不设限。
                // 卡池这一组同样如此：一个池都没选（默认状态）必须显示全部候选牌 —— 图鉴那边是一个都没选就空屏。
                if (!MatchesGroup(_typeFilters, card)
                    || !MatchesGroup(_rarityFilters, card)
                    || !MatchesGroup(_costFilters, card)
                    || !MatchesGroup(_poolFilters, card))
                {
                    continue;
                }

                if (query.Length > 0 && !MatchesQuery(card, query))
                {
                    continue;
                }

                filtered.Add(card);
            }

            // 传 SortingOrders.Ascending 时 SetCards 保持传入顺序（只有点了排序按钮才会真正排序）
            _grid.SetCards(filtered, PileType.None, _sorting);

            if (_noResultsLabel is not null && GodotObject.IsInstanceValid(_noResultsLabel))
            {
                _noResultsLabel.Visible = filtered.Count == 0;
            }
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[创造模式] 选牌屏搜索：重排卡牌失败：{e}", 1);
        }
    }

    private static bool MatchesGroup(List<(Func<CardModel, bool> Matches, Func<bool> IsTicked)> group, CardModel card)
    {
        if (group.Count == 0)
        {
            return true;
        }

        bool anyTicked = false;
        foreach ((Func<CardModel, bool> matches, Func<bool> isTicked) in group)
        {
            if (!isTicked())
            {
                continue;
            }

            anyTicked = true;
            if (matches(card))
            {
                return true;
            }
        }

        // 一个都没勾 => 该组不设限；勾了但都不匹配 => 排除
        return !anyTicked;
    }

    private static bool MatchesQuery(CardModel card, string query)
    {
        // 与图鉴一致：卡名 + 描述正文（去掉富文本标记）一起匹配。
        // 区别是这里**不**要求“已在图鉴中发现过”——选牌屏的候选本来就该全能搜到。
        string haystack;
        try
        {
            haystack = NSearchBar.Normalize(
                card.Title + " " + NSearchBar.RemoveHtmlTags(card.GetDescriptionForPile(PileType.None).StripBbCode()));
        }
        catch (Exception)
        {
            // 个别牌取不到描述（比如被别的 mod 改坏的牌）时退回只按卡名匹配，
            // 不能让一张牌把整屏的搜索都弄挂
            haystack = NSearchBar.Normalize(card.Title);
        }

        return haystack.Contains(query, StringComparison.Ordinal);
    }
}
