using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using System.Runtime.CompilerServices;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PowerReceived))]
public static class ZilongDebuffHistoryPatch
{
    private sealed class Counter
    {
        public int Count;
        public HashSet<ZilongLocked> Cards { get; } = new();
        public HashSet<DeafenMe> OmenCards { get; } = new();
    }

    private static readonly ConditionalWeakTable<object, Counter> Counts = new();

    internal static int CountFor(object state) => Counts.TryGetValue(state, out var counter) ? counter.Count : 0;

    internal static int Track(object state, ZilongLocked card)
    {
        var counter = Counts.GetValue(state, static _ => new Counter());
        counter.Cards.Add(card);
        return counter.Count;
    }

    internal static void Track(object state, DeafenMe card) =>
        Counts.GetValue(state, static _ => new Counter()).OmenCards.Add(card);

    public static void Postfix(PowerModel power, decimal amount)
    {
        int change = (int)amount;
        if (power.Owner.Player?.PlayerCombatState is not { } state || change == 0)
            return;

        bool gained = change > 0
            ? power.Type == PowerType.Debuff && power.Amount + change > 0
            : power.AllowNegative && power.GetTypeForAmount(power.Amount + change) == PowerType.Debuff &&
                power.Amount + change < 0;
        if (!gained)
            return;

        var counter = Counts.GetValue(state, static _ => new Counter());
        counter.Count++;
        foreach (var card in counter.Cards.ToArray())
            card.RefreshDiscount();
        foreach (var card in counter.OmenCards.ToArray())
            card.RefreshOmenPreview();
    }
}
