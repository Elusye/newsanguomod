using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Patches;

/// <summary>在卡牌离开手牌前快照相邻地域，保持同一次出牌的重放次数稳定。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class TerritoryReplayPatch
{
    private static readonly ConditionalWeakTable<CardModel, Stack<int>> Snapshots = new();
    internal static int CountNeighbors(CardModel card)
    {
        if (!card.IsMutable || card.Owner?.PlayerCombatState is not { } state) return 0;
        var hand = state.Hand.Cards;
        int index = hand.ToList().IndexOf(card);
        if (index < 0) return 0;
        int count = 0;
        if (index > 0 && hand[index - 1] is InvincibleWithinThreeHundredLi left) count += left.GrantedReplay;
        if (index + 1 < hand.Count && hand[index + 1] is InvincibleWithinThreeHundredLi right) count += right.GrantedReplay;
        return count;
    }
    internal static int ReplayBonus(CardModel card) =>
        Snapshots.TryGetValue(card, out var stack) && stack.Count > 0 ? stack.Peek() : CountNeighbors(card);
    public static void Prefix(CardModel __instance)
    {
        int count = CountNeighbors(__instance);
        Snapshots.GetOrCreateValue(__instance).Push(count);
    }
    public static void Postfix(CardModel __instance, ref Task __result) => __result = Complete(__instance, __result);
    private static async Task Complete(CardModel card, Task task)
    {
        try { await task; }
        finally
        {
            if (Snapshots.TryGetValue(card, out var stack))
            {
                stack.Pop();
                if (stack.Count == 0) Snapshots.Remove(card);
            }
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetEnchantedReplayCount))]
public static class TerritoryReplayCountPatch
{
    public static void Postfix(CardModel __instance, ref int __result) =>
        __result += TerritoryReplayPatch.ReplayBonus(__instance);
}

// 手牌增减也会改变相邻关系，及时刷新原版的重放描述和悬浮提示。
[HarmonyPatch(typeof(CardPile), nameof(CardPile.InvokeContentsChanged))]
public static class TerritoryHandReplayRefreshPatch
{
    public static void Postfix(CardPile __instance)
    {
        if (__instance.Type != PileType.Hand) return;
        foreach (var card in __instance.Cards.ToArray())
            card.BaseReplayCount = card.BaseReplayCount;
    }
}

/// <summary>只在真实出牌计算出重放次数后播放古城音效，预览和消耗地域牌不播放。</summary>
[HarmonyPatch(typeof(CardModel), "GeneratePlayCount")]
public static class TerritoryReplaySoundPatch
{
    public static void Postfix(CardModel __instance, ref Task<int> __result)
    {
        if (__instance is not TerritoryCardTemplate && TerritoryReplayPatch.ReplayBonus(__instance) > 0)
            __result = PlayForReplay(__result);
    }
    private static async Task<int> PlayForReplay(Task<int> original)
    {
        int count = await original;
        if (count > 1)
            NewsanguoSfx.Play("event:/newsanguo/sfx/invincible_within_three_hundred_li");
        return count;
    }
}
