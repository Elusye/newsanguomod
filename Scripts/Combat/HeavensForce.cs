using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Combat;

/// <summary>
/// “天意之力”：本角色专属的次级战斗资源（战斗 UI 中显示为能量计数器旁的数字）。
/// 数值真值存放在这里，而不是能力层数；可以是负数（≤-10 会在回合结束时触发“天意侵蚀”）。
/// 回合结束的转化与额外回合由隐藏的逻辑载体 <see cref="HeavensForcePower"/> 负责：
/// 载体在数值首次不为 0 时挂载，此后整个战斗期间保留（同时兼作“本场战斗累计失去的天意之力”账本），
/// 数值为 0 时回合结束无事可做。
/// </summary>
public static class HeavensForce
{
    // 模组内资源 id（注册后完整 id 见 Definition.Id）
    public const string LocalId = "heavens_force";

    // 本地化：文本放在原版 static_hover_tips 表（模组的 localization 目录只能合并进原版已有的表，
    // 新表名不会被加载；RitsuLib 的模组悬停提示文本同样约定放在该表）
    private const string LocTable = "static_hover_tips";
    private const string TitleKey = "NEWSANGUO_SECONDARY_RESOURCE_HEAVENS_FORCE.title";
    // 简略版：战斗外显示
    private const string DescriptionKey = "NEWSANGUO_SECONDARY_RESOURCE_HEAVENS_FORCE.description";
    // 精确版：战斗中显示
    private const string SmartDescriptionKey = "NEWSANGUO_SECONDARY_RESOURCE_HEAVENS_FORCE.smartDescription";
    private const string SmallIconPath = "res://newsanguo/images/secondary_resource/HeavensForce.png";
    private const string LargeIconPath = "res://newsanguo/images/secondary_resource/HeavensForceBig.png";

    // 阈值高亮的触发点：≥10 金色（即将转化为额外回合）、≤-10 红色（即将被天意侵蚀）
    // 与 HeavensForcePower 回合末转化所用的 ±10 一致
    private const int Threshold = 10;
    // 高亮颜色取自原版卡牌高亮（gold / red），保持与卡牌高亮一致的观感
    private static readonly Color GoldHighlight = NCardHighlight.gold with { A = 1f };
    private static readonly Color RedHighlight = NCardHighlight.red with { A = 1f };

    public static SecondaryResourceDefinition Definition { get; private set; } = null!;
    public static string Id { get; private set; } = string.Empty;

    public static void Register()
    {
        ModSecondaryResourceRegistry resources = RitsuLibFramework.GetSecondaryResourceRegistry(Entry.ModId);

        Definition = resources.Register(
            LocalId,
            new SecondaryResourceDefinition(
                defaultAmount: 0,
                baseMaxAmount: null,
                // 硬下限设为极大负数：天意之力允许透支为负（正能量方向触发额外回合、负方向触发天意侵蚀）
                minAmount: -999999999,
                turnStartPolicy: SecondaryResourceTurnStartPolicy.None,
                persistencePolicy: SecondaryResourcePersistencePolicy.Combat,
                // 标题/说明：计数器只在战斗中出现，故定义里指向精确版
                locTable: LocTable,
                titleKey: TitleKey,
                descriptionKey: SmartDescriptionKey,
                smallIconPath: SmallIconPath,
                largeIconPath: LargeIconPath
            )
        );

        Id = Definition.Id;

        // 战斗 UI：把计数器放在能量计数器旁边
        resources.RegisterCombatUi(
            $"{LocalId}_counter",
            parent =>
            {
                NSecondaryResourceCounter row = NSecondaryResourceCounter.Create(
                    Definition,
                    new SecondaryResourceCounterStyle
                    {
                        FontSize = 27,
                        OutlineSize = 14,
                        AmountLabelOffset = new Vector2(2.25f, 0),
                        FormatAmount = (amount, _) => amount.ToString(),
                        CounterSize = new Vector2(72, 72),
                        IconSize = new Vector2(68, 68),
                        IconStyle = SecondaryResourceIconStyle.Default with
                        {
                            Size = new Vector2(72, 72),
                            HoverTip = SecondaryResourceHoverTipStyle.Default,
                        },
                        GainFeedback = new SecondaryResourceCounterGainFeedback
                        {
                            Effects = [SecondaryResourceCounterGainEffects.StarCounterLikeBurst()],
                        },
                    }
                );
                // 调整其位置
                Control energyCounter = parent.GetNode<Control>("%EnergyCounterContainer");
                row.Position = energyCounter.Position + new Vector2(92f, -44f);
                return row;
            },
            ctx =>
            {
                ctx.Node.Bind(ctx.Player);
                // 绑定/重新绑定时先同步一次高亮（战斗开始时数值为 0，即无高亮）
                ApplyThresholdHighlight(ctx.Node, Get(ctx.Player));
            },
            // 数值变化时刷新阈值高亮
            changeCtx => ApplyThresholdHighlight(changeCtx.Node, changeCtx.NewAmount)
        );

        // 只在本角色下常驻显示（数值为 0 时也显示，便于玩家看到“透支”进度）
        resources.AlwaysShowInCombatUiForCharacter<NewsanguoCharacter>(LocalId);
    }

    // 当前天意之力（非玩家生物或未参战时为 0）
    public static int Get(Player? player)
    {
        return player is null ? 0 : SecondaryResourceCmd.Get(player, Id);
    }

    // 阈值高亮：数值 ≥10 时整体染金，≤-10 时整体染红，其余恢复原色
    private static void ApplyThresholdHighlight(CanvasItem counter, int amount)
    {
        counter.Modulate = amount >= Threshold
            ? GoldHighlight
            : amount <= -Threshold
                ? RedHighlight
                : Colors.White;
    }

    // 本场战斗累计失去的天意之力（“天意修正”按此结算额外伤害；不包含额外回合的转化扣减）
    public static int LostThisCombat(Player? player)
    {
        return player?.Creature.GetPower<HeavensForcePower>()?.LostThisCombat ?? 0;
    }

    // 本场战斗累计获得的天意之力（“天意修正”按此结算额外伤害；不包含天意侵蚀的转化回升）
    public static int GainedThisCombat(Player? player)
    {
        return player?.Creature.GetPower<HeavensForcePower>()?.GainedThisCombat ?? 0;
    }

    // 判断某个悬停提示是否为“天意之力”资源自身的提示
    // （资源提示由 RitsuLib 生成，id 与图标都指向本资源；供 HeavensForceHoverTipPatch 追加“天意侵蚀”说明）
    internal static bool IsOwnHoverTip(IHoverTip tip)
    {
        // 资源提示的 id 由 RitsuLib 依据资源定义生成，其中包含资源 id 或本资源的本地化键
        if (tip.Id?.Contains(LocalId, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        // 兜底：图标指向本资源的贴图
        if (tip is not HoverTip hoverTip)
        {
            return false;
        }

        string? iconPath = hoverTip.Icon?.ResourcePath;
        return iconPath is not null
            && (iconPath.EndsWith(SmallIconPath, StringComparison.Ordinal)
                || iconPath.EndsWith(LargeIconPath, StringComparison.Ordinal));
    }

    // 悬浮提示（卡牌/遗物/能力说明中展示“天意之力”）
    // 与原版能力的 smartDescription 规则一致：战斗中显示精确版，战斗外（牌组查看、卡牌奖励等）显示简略版
    public static IHoverTip HoverTip()
    {
        if (CombatManager.Instance.IsInProgress)
        {
            return SecondaryResourceHoverTipFactory.Create(Definition, 0);
        }

        return new HoverTip(
            new LocString(LocTable, TitleKey),
            new LocString(LocTable, DescriptionKey),
            ResourceLoader.Load<Texture2D>(SmallIconPath));
    }

    /// <summary>
    /// 按增量变动天意之力（正为获得、负为失去）。
    /// </summary>
    /// <param name="source">
    /// 引发变动的模型（通常是打出中的卡牌）。用于“魔法禁术目录”的本回合免消耗判定。
    /// </param>
    public static async Task Add(PlayerChoiceContext choiceContext, Player? player, int delta, AbstractModel? source = null)
    {
        if (player is null || delta == 0)
        {
            return;
        }

        // “魔法禁术目录”：被标记的禁术牌在其加入手牌的那个回合打出时不消耗天意之力
        if (delta < 0 && source is NewsanguoCardTemplate { IsFreeHeavensForceThisTurn: true })
        {
            return;
        }

        if (delta > 0)
        {
            await Gain(choiceContext, player, delta, source);
        }
        else
        {
            await Lose(choiceContext, player, -delta, source);
        }
    }

    public static async Task Gain(PlayerChoiceContext choiceContext, Player? player, int amount, AbstractModel? source = null,
        bool recordGain = true)
    {
        if (player is null || amount <= 0)
        {
            return;
        }

        await SecondaryResourceCmd.Gain(player, Id, amount, source);
        HeavensForcePower? power = await SyncPower(choiceContext, player);

        if (recordGain)
        {
            power?.RecordGain(amount);
        }
    }

    /// <summary>
    /// 失去天意之力。
    /// </summary>
    /// <param name="recordLoss">
    /// 是否计入“本场战斗累计失去的天意之力”。额外回合的转化扣减属于内部结算，传 false。
    /// </param>
    public static async Task Lose(PlayerChoiceContext choiceContext, Player? player, int amount, AbstractModel? source = null,
        bool recordLoss = true)
    {
        if (player is null || amount <= 0)
        {
            return;
        }

        await SecondaryResourceCmd.Lose(player, Id, amount, source);
        HeavensForcePower? power = await SyncPower(choiceContext, player);

        if (recordLoss)
        {
            power?.RecordLoss(amount);
        }
    }

    // 直接设定天意之力（用于“酒治百病”把负天意之力归零等场景）
    public static async Task Set(PlayerChoiceContext choiceContext, Player? player, int amount, AbstractModel? source = null)
    {
        if (player is null)
        {
            return;
        }

        await SecondaryResourceCmd.Set(player, Id, amount, source);
        await SyncPower(choiceContext, player);
    }

    // 数值不为 0 时确保逻辑载体在场。载体在整个战斗期间保留（兼作账本），不做卸下。
    private static async Task<HeavensForcePower?> SyncPower(PlayerChoiceContext choiceContext, Player player)
    {
        HeavensForcePower? power = player.Creature.GetPower<HeavensForcePower>();
        if (power is null && SecondaryResourceCmd.Get(player, Id) != 0)
        {
            Creature creature = player.Creature;
            power = await PowerCmd.Apply<HeavensForcePower>(choiceContext, creature, 1, creature, null, silent: true);
        }

        return power;
    }
}
