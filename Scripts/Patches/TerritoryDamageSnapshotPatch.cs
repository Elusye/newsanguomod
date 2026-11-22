using newsanguo.Scripts.Settings;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Patches;

// 出牌离开手牌前记录相邻关系，供整次出牌的伤害计算使用。
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class TerritoryDamageSnapshotPatch
{
    private sealed record Snapshot(InvincibleWithinThreeHundredLi? Territory, int Replay)
    {
        public bool PlayedSound { get; set; }
    }

    private static readonly ConditionalWeakTable<CardModel, Stack<Snapshot>> Snapshots = new();

    private static Snapshot FindNeighbors(CardModel card)
    {
        if (!card.IsMutable || card.Owner?.PlayerCombatState is not { } state)
        {
            return new Snapshot(null, 0);
        }

        var hand = state.Hand.Cards;
        int index = hand.ToList().IndexOf(card);
        if (index < 0)
        {
            return new Snapshot(null, 0);
        }

        var left = index > 0 ? hand[index - 1] as InvincibleWithinThreeHundredLi : null;
        var right = index + 1 < hand.Count ? hand[index + 1] as InvincibleWithinThreeHundredLi : null;
        return new Snapshot(left ?? right, (left?.GrantedReplay ?? 0) + (right?.GrantedReplay ?? 0));
    }

    internal static InvincibleWithinThreeHundredLi? GetNeighbor(CardModel card) =>
        Snapshots.TryGetValue(card, out var stack) && stack.TryPeek(out var snapshot)
            ? snapshot.Territory
            : FindNeighbors(card).Territory;

    internal static int GetReplay(CardModel card) =>
        Snapshots.TryGetValue(card, out var stack) && stack.TryPeek(out var snapshot)
            ? snapshot.Replay
            : FindNeighbors(card).Replay;

    internal static void PlaySound(CardModel card, InvincibleWithinThreeHundredLi territory)
    {
        if (Snapshots.TryGetValue(card, out var stack) && stack.TryPeek(out var snapshot) &&
            snapshot.Territory == territory && !snapshot.PlayedSound)
        {
            snapshot.PlayedSound = true;
            NewsanguoSfx.Play("event:/newsanguo/sfx/invincible_within_three_hundred_li");
        }
    }

    public static void Prefix(CardModel __instance) =>
        Snapshots.GetOrCreateValue(__instance).Push(FindNeighbors(__instance));

    public static void Postfix(CardModel __instance, ref Task __result) => __result = Complete(__instance, __result);

    private static async Task Complete(CardModel card, Task task)
    {
        try
        {
            await task;
        }
        finally
        {
            if (Snapshots.TryGetValue(card, out var stack))
            {
                stack.Pop();
                if (stack.Count == 0)
                {
                    Snapshots.Remove(card);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.GetEnchantedReplayCount))]
public static class TerritoryLegacyReplayPatch
{
    public static void Postfix(CardModel __instance, ref int __result)
    {
        __result += TerritoryDamageSnapshotPatch.GetReplay(__instance);
    }
}

[HarmonyPatch(typeof(CardPile), nameof(CardPile.InvokeContentsChanged))]
public static class TerritoryLegacyReplayRefreshPatch
{
    public static void Postfix(CardPile __instance)
    {
        if (__instance.Type != PileType.Hand) return;
        foreach (var card in __instance.Cards.ToArray())
            card.BaseReplayCount = card.BaseReplayCount;
    }
}


[HarmonyPatch(typeof(CardModel), "GeneratePlayCount")]
public static class TerritoryLegacyReplaySoundPatch
{
    public static void Postfix(CardModel __instance, ref Task<int> __result)
    {
        if (__instance is not TerritoryCardTemplate &&
            TerritoryDamageSnapshotPatch.GetReplay(__instance) > 0)
        {
            __result = PlayForReplay(__result);
        }
    }

    private static async Task<int> PlayForReplay(Task<int> original)
    {
        int count = await original;
        if (count > 1)
            NewsanguoSfx.Play("event:/newsanguo/sfx/invincible_within_three_hundred_li");
        return count;
    }
}
