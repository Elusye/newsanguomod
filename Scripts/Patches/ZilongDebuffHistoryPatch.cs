using MegaCrit.Sts2.Core.Entities.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using System.Runtime.CompilerServices;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPowerAmountChanged))]
public static class ZilongDebuffHistoryPatch
{
    private sealed class Counter
    {
        public int Count;
        public HashSet<ZilongLocked> Cards { get; } = new();
        public HashSet<DeafenMe> OmenCards { get; } = new();
    }

    private static readonly ConditionalWeakTable<object, ConditionalWeakTable<object, Counter>> Combats = new();
    private static ConditionalWeakTable<object, Counter> CountsFor(object combat) =>
        Combats.GetValue(combat, static _ => new());

    internal static int CountFor(object state, object combat) => CountsFor(combat).TryGetValue(state, out var counter) ? counter.Count : 0;

    internal static int Track(object state, ZilongLocked card)
    {
        var counter = CountsFor(card.Owner.Creature.CombatState!).GetValue(state, static _ => new Counter());
        counter.Cards.Add(card);
        return counter.Count;
    }

    internal static void Track(object state, DeafenMe card) =>
        CountsFor(card.Owner.Creature.CombatState!).GetValue(state, static _ => new Counter()).OmenCards.Add(card);

    internal static bool IsGainedDebuff(PowerModel power, decimal amount, int before) =>
        power.TypeForCurrentAmount == PowerType.Debuff &&
        (amount > 0 && power.Amount > 0 && power.Amount > before ||
         amount < 0 && power.Amount < 0 && power.Amount < Math.Min(before, 0));

    internal static readonly ConditionalWeakTable<PowerModel, PreviousAmount> Previous = new();
    internal sealed class PreviousAmount { public int Value; }

    public static void Prefix(PowerModel power, decimal amount)
    {
        if (!Previous.TryGetValue(power, out var previous)) return;
        int before = previous.Value;
        Previous.Remove(power);
        if (power.Owner.Player?.PlayerCombatState is not { } state ||
            !IsGainedDebuff(power, amount, before)) return;

        var counter = CountsFor(power.Owner.CombatState!).GetValue(state, static _ => new Counter());
        counter.Count++;
        // 直接从当前牌堆取得实际参战实例，不依赖入场回调留下的引用。
        var player = power.Owner.Player!;
        foreach (var pileType in new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust, PileType.Play })
        {
            foreach (var card in pileType.GetPile(player).Cards.ToArray())
            {
                if (card is ZilongLocked zilong) counter.Cards.Add(zilong);
                if (card is DeafenMe omen) counter.OmenCards.Add(omen);
            }
        }
        GD.Print($"[newsanguo] Debuff gained: {power.Id}, {before}->{power.Amount}, count={counter.Count}, Zilong={counter.Cards.Count}");
        foreach (var card in counter.Cards.ToArray())
            card.RefreshDiscount();
        foreach (var card in counter.OmenCards.ToArray())
            card.RefreshOmenPreview();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforePowerAmountChanged))]
public static class ZilongDebuffBeforeChangePatch
{
    public static void Prefix(PowerModel power) =>
        ZilongDebuffHistoryPatch.Previous.GetValue(power, static _ => new()).Value = power.Amount;
}
