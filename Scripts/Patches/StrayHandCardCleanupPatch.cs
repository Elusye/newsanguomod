using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace newsanguo.Scripts.Patches;

// “卡牌错位”（逻辑上在手牌、画面上却停在屏幕中央）的归位清扫。
//
// 引擎机制（v0.111 实测反编译，行号可直接核对）：
//  1) 点出牌 → 牌进动作队列时，NCardPlayQueue.OnLocalCardPlayed(:122-148) 会把卡牌**节点**从手牌
//     容器搬进 NCardPlayQueue（位置＝出牌堆位置＋左偏 300·n，即“屏幕中央偏左”），而 CardModel
//     仍留在手牌堆 —— 引擎自己的注释见 NPlayerHand.cs:431-444（“前端漂浮在手牌之外、后端仍算手牌”）。
//  2) 只有两条路会把它收回去：
//     · 正常执行：OnPlayWrapper → CardPileCmd.AddDuringManualCardPlay → 节点进 PlayContainer；
//     · 取消：PlayCardAction.CancelAction(:110-128) → NCardPlayQueue.RemoveCardFromQueueForCancellation
//       （NCardPlayQueue.cs:276-296：仅当“队列项仍在 _playQueue 里 + 本地玩家 + 模型仍在 Hand”
//        才调 NPlayerHand.Add，否则只做淡出）＋ NPlayerHand.TryCancelCardPlay(:749-765：仅对仍挂在
//        _holdersAwaitingQueue 里的 holder 生效)。
//     任一条不满足（队列项已被移除、holder 早已被回收、非本地牌…），节点就永远停在被 tween 到的
//     中央位置 —— 这就是玩家看到的“卡牌悬在屏幕中央”。
//  3) 选牌（CardSelectCmd.FromHand）**正常结束**时，“把选中的牌收回手牌”这一步被挂在
//     source.ExecutionFinished 上（NPlayerHand.AfterCardsSelected:1053-1059 → OnSelectModeSourceFinished
//     :1272-1289）。若来源执行永远不结束（出牌被取消、OnPlay 抛异常、玩家死亡提前 return），
//     选中的牌就永远留在选牌容器（屏幕中央）里；而“中途取消选择”那条路（CancelHandSelectionIfNecessary
//     :1063-1070 → AfterCardsSelected(null)）原版已会立刻归还。
//
// 本补丁只做“归位清扫”：不改变任何游戏逻辑、数值与时序，只把已经错位的节点搬回手牌容器，
// 并且只处理“CardModel 仍在手牌堆 + 属于本机玩家”的牌（已被打出/弃掉/消耗/变化的牌一律不动）。
internal static class StrayHandCardCleanup
{
    // NPlayerHand._selectedHandCardContainer（private）：选牌时被选中的牌所在的“屏幕中央”容器。
    private static readonly FieldInfo? SelectedContainerField =
        AccessTools.Field(typeof(NPlayerHand), "_selectedHandCardContainer");

    // 在“选牌进行中”被跳过的牌：等选牌结束 / 战斗结束 / 动作队列空闲后再补一次。
    // 用 List 而非 HashSet：CardModel 的相等语义不经保证，这里只需要按引用去重。
    private static readonly List<CardModel> Pending = new();

    /// <summary>
    /// 把一张“模型已经回到手牌堆、节点却不在手牌容器里”的牌收回手牌（幂等，可随时调用）。
    /// 只在“确定这张牌不会再被来源效果消费”的时机调用（出牌被取消 / 战斗结束）。
    /// </summary>
    public static void ReturnIfStranded(CardModel? model)
    {
        if (model is null)
        {
            return;
        }
        try
        {
            if (TryReturnIfStranded(model, allowSelectedContainerFlush: true))
            {
                Pending.Remove(model);
            }
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[StrayCard] 归位清扫异常: {e}");
        }
    }

    /// <summary>
    /// 补一次“选牌进行中被跳过”的牌。
    /// </summary>
    /// <param name="allowSelectedContainerFlush">
    /// 是否允许顺带清空选牌容器。只有“出牌被取消 / 战斗结束”这类确定安全的时机才传 true：
    /// 玩家刚确认选牌、来源效果还没把牌弃掉/变化完时清空容器，会把牌提前抢回手牌。
    /// </param>
    public static void SweepPending(bool allowSelectedContainerFlush)
    {
        // 逆序遍历，边补边清
        for (int i = Pending.Count - 1; i >= 0; i--)
        {
            CardModel model = Pending[i];
            try
            {
                if (model.Pile?.Type != PileType.Hand
                    || TryReturnIfStranded(model, allowSelectedContainerFlush))
                {
                    Pending.RemoveAt(i);
                }
            }
            catch (Exception e)
            {
                Pending.RemoveAt(i);
                Diagnostics.Log($"[StrayCard] 待办清扫异常: {e}");
            }
        }
    }

    /// <summary>
    /// 动作队列是否空闲（没有动作正在执行/排队）。空闲才代表“来源牌的 OnPlay 已经结束”，
    /// 此时清空选牌容器不会抢在来源效果之前。
    /// </summary>
    public static bool IsActionQueueIdle()
    {
        try
        {
            return RunManager.Instance?.ActionQueueSet is not { } set || set.IsEmpty;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 把选牌容器（屏幕中央）里“模型仍算在手牌”的残留牌收回手牌。
    /// 已被弃掉/消耗/变化的牌不动——它们的节点由引擎自己的动画收尾。
    /// </summary>
    public static void FlushSelectedContainer()
    {
        try
        {
            NPlayerHand? hand = NPlayerHand.Instance;
            if (hand is null || hand.IsInCardSelection)
            {
                return; // 选牌进行中：中央的牌是设计如此，不能动
            }
            if (SelectedContainerField?.GetValue(hand) is not NSelectedHandCardContainer container)
            {
                return;
            }
            foreach (NSelectedHandCardHolder holder in container.Holders.ToList())
            {
                if (!GodotObject.IsInstanceValid(holder))
                {
                    continue;
                }
                NCard? node = holder.CardNode;
                CardModel? model = node?.Model;
                if (node is null || model is null)
                {
                    continue;
                }
                Player? owner = model.Owner;
                if (owner is null || !LocalContext.IsMe(owner) || model.Pile?.Type != PileType.Hand)
                {
                    continue; // 只管本机玩家、且确实还在手牌堆的牌
                }
                // 与引擎 OnSelectModeSourceFinished(:1272-1289) 完全相同的收尾方式
                holder.QueueFreeSafely();
                hand.Add(node);
                Diagnostics.Log($"[StrayCard] 已把选牌容器里的 {model.Id.Entry} 收回手牌");
            }
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[StrayCard] 选牌容器归位失败: {e}");
        }
    }

    /// <returns>true 表示“这张牌不需要再兜底了”。</returns>
    private static bool TryReturnIfStranded(CardModel model, bool allowSelectedContainerFlush)
    {
        // 只处理本机玩家的手牌：其他玩家的牌在自己屏幕上只是意图/队列视觉，不归手牌容器管
        Player? owner = model.Owner;
        if (owner is null || !LocalContext.IsMe(owner))
        {
            return true;
        }
        if (model.Pile?.Type != PileType.Hand)
        {
            return true; // 已经被打出（Play 堆）/弃掉/消耗：不是“逻辑上还在手牌”的错位
        }
        NPlayerHand? hand = NPlayerHand.Instance;
        if (hand is null)
        {
            return true; // 不在战斗里
        }
        if (hand.IsInCardSelection)
        {
            // 选牌进行中：中央的牌是设计如此；记下来等选牌结束再补
            if (!Pending.Contains(model))
            {
                Pending.Add(model);
            }
            return false;
        }

        NCardHolder? holder = hand.GetCardHolder(model);
        if (holder is NHandCardHolder inHand && inHand.GetParent() == hand.CardHolderContainer)
        {
            return true; // 已经好好在手牌容器里
        }
        if (holder is NSelectedHandCardHolder)
        {
            if (!allowSelectedContainerFlush)
            {
                return false; // 选牌容器里的牌：等一个“确定来源不会再消费它们”的时机
            }
            FlushSelectedContainer();
            return true;
        }
        if (holder is NHandCardHolder awaiting && hand.IsAwaitingPlay(awaiting))
        {
            return true; // 正在拖拽 / 等待出牌：交给引擎，别抢
        }
        if (NCardPlayQueue.Instance?.GetCardNode(model) is not null)
        {
            return true; // 仍在出牌队列里（正常的“待出牌”状态）
        }

        NCard? node = holder?.CardNode ?? NCard.FindOnTable(model);
        if (node is null || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree())
        {
            return true; // 没有节点可搬（或已被回收）
        }

        // 引擎自己的归位入口：新建 holder → Reparent 节点 → RefreshLayout
        hand.Add(node);
        Diagnostics.Log($"[StrayCard] 已把 {model.Id.Entry} 的节点收回手牌（原 holder={holder?.GetType().Name ?? "无(节点游离)"}）");
        return true;
    }
}

// ① 取消排队出牌时（PlayCardAction.CancelAction / CardPileCmd / CardCmd 都会走到这里），
//    在引擎自己的收尾之后再补一次归位。
[HarmonyPatch(typeof(NCardPlayQueue), nameof(NCardPlayQueue.RemoveCardFromQueueForCancellation), new[] { typeof(NCard), typeof(bool) })]
public static class StrayCardQueueCancelPatch
{
    public static void Postfix(NCard card)
    {
        StrayHandCardCleanup.ReturnIfStranded(card?.Model);
    }
}

// ①' 任何 PlayCardAction 被取消之后兜一次。覆盖“引擎连队列项都没找到、于是完全没做收尾”的情况
//     （例如出牌执行阶段 CanPlay/目标校验失败导致的静默 Cancel，见 PlayCardAction.cs:85-88）。
[HarmonyPatch(typeof(PlayCardAction), "CancelAction")]
public static class StrayCardPlayCancelPatch
{
    public static void Postfix(PlayCardAction __instance)
    {
        try
        {
            if (!LocalContext.IsMe(__instance.Player))
            {
                return;
            }
            StrayHandCardCleanup.ReturnIfStranded(__instance.NetCombatCard.ToCardModelOrNull());
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[StrayCard] 取消兜底失败: {e}");
        }
    }
}

// ② 选牌结束：
//    · 立刻补一次“选牌期间被跳过”的游离节点（不动选牌容器——来源效果马上要消费它们）；
//    · 正常结束时原版把“归还选中的牌”挂在 source.ExecutionFinished 上。这里额外挂一个 1.5 秒的
//      兜底定时器：只有当**动作队列已经空闲**（= 来源牌的 OnPlay 确实结束了，而不是还在跑）时，
//      才清空选牌容器，避免抢在来源效果（弃牌/变化）之前把牌收回手牌。
[HarmonyPatch(typeof(NPlayerHand), "AfterCardsSelected")]
public static class StrayCardSelectionEndPatch
{
    private const double FallbackDelaySeconds = 1.5;

    public static void Postfix(AbstractModel? source)
    {
        StrayHandCardCleanup.SweepPending(allowSelectedContainerFlush: false);
        if (source is null)
        {
            return; // 取消路径：原版已立刻归还（AfterCardsSelected 的 else 分支）
        }
        try
        {
            SceneTree? tree = NPlayerHand.Instance?.GetTree();
            if (tree is null)
            {
                return;
            }
            tree.CreateTimer(FallbackDelaySeconds).Timeout += () =>
            {
                StrayHandCardCleanup.SweepPending(allowSelectedContainerFlush: false);
                if (StrayHandCardCleanup.IsActionQueueIdle())
                {
                    StrayHandCardCleanup.FlushSelectedContainer();
                }
            };
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[StrayCard] 选牌兜底定时器创建失败: {e}");
        }
    }
}

// ②' 战斗结束（手牌收场）时再兜一次：玩家死亡 / 战斗提前结束时，上面那个定时器可能已经来不及。
//     此时战斗已经结束，不存在“来源效果还没消费完”的问题，可以安全清空选牌容器。
[HarmonyPatch(typeof(NPlayerHand), nameof(NPlayerHand.AnimOut))]
public static class StrayCardCombatEndPatch
{
    public static void Postfix()
    {
        StrayHandCardCleanup.SweepPending(allowSelectedContainerFlush: true);
        StrayHandCardCleanup.FlushSelectedContainer();
    }
}
