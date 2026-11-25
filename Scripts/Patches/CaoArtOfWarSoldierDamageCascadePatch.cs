using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using newsanguo.Scripts.Monsters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPreBlockDamageTarget))]
public static class QingzhouSoldierPreBlockTargetPatch
{
    public static void Postfix(Creature originalTarget, ValueProp props, ref Creature? __result)
    {
        if (CaoArtOfWarSoldierDamageCascadePatch.Recalculating)
        {
            __result = null;
            return;
        }
        if (originalTarget.IsAlive && props.IsPoweredAttack())
            __result = CaoArtOfWarPower.GetLivingSoldiersInSummonOrder(originalTarget).FirstOrDefault() ?? __result;
    }
}

// 0.114：只在试算阶段构建额外结果，实际扣血和回调在结算阶段执行。
[HarmonyPatch(typeof(CreatureCmd), "GetDamageResults")]
public static class CaoArtOfWarSoldierDamageCascadePatch
{
    [ThreadStatic] internal static bool Recalculating;
    private static readonly ConditionalWeakTable<DamageResult, List<DamageResult>> ExtraResults = new();
    private static readonly MethodInfo Calculate = AccessTools.Method(typeof(CreatureCmd), "GetDamageResults");

    public static void Postfix(IRunState runState, ICombatState? combatState, Creature target,
        ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, ref DamageResults __result)
    {
        if (Recalculating || __result.preBlockRedirect is not { } first ||
            first.Receiver.Monster is not QingzhouSoldier || first.Receiver.PetOwner?.Creature != target ||
            first.OverkillDamage <= 0) return;

        int remaining = first.OverkillDamage;
        List<DamageResult> extras = [];
        foreach (var soldier in CaoArtOfWarPower.GetLivingSoldiersInSummonOrder(target))
        {
            if (remaining <= 0) break;
            if (soldier == first.Receiver || !soldier.IsAlive) continue;
            var loss = soldier.LoseHpInternal(remaining, dryRun: true);
            extras.Add(new DamageResult(soldier, props)
            {
                OriginalTarget = target,
                IntendedHpLoss = remaining,
                UnblockedDamage = loss.hpLost,
                WasTargetKilled = loss.killed,
                OverkillDamage = loss.overkillAmount
            });
            remaining = loss.overkillAmount;
        }
        if (extras.Count == 0) return;

        // 前置承伤和 BeforeWuppy 限额已结算，只重算剩余伤害的格挡／奥斯提部分。
        Recalculating = true;
        DamageResults rest;
        try
        {
            rest = (DamageResults)Calculate.Invoke(null,
                [runState, combatState, target, (decimal)remaining, props, dealer, cardSource, cardPlay])!;
        }
        finally { Recalculating = false; }
        rest.preBlockRedirect = first;
        rest.beforeWuppyHpLossModifiers = __result.beforeWuppyHpLossModifiers;
        ExtraResults.Add(first, extras);
        __result = rest;
    }

    internal static IReadOnlyList<DamageResult> Extras(DamageResults results) =>
        results.preBlockRedirect is { } first && ExtraResults.TryGetValue(first, out var extras)
            ? extras : Array.Empty<DamageResult>();
}

[HarmonyPatch(typeof(DamageResults), nameof(DamageResults.AllResults))]
public static class QingzhouSoldierExtraDamageResultsPatch
{
    public static void Postfix(DamageResults __instance, ref IEnumerable<DamageResult> __result)
    {
        var extras = CaoArtOfWarSoldierDamageCascadePatch.Extras(__instance);
        if (extras.Count > 0) __result = __result.Concat(extras);
    }
}

[HarmonyPatch(typeof(CreatureCmd), "ApplyDamageResults")]
public static class QingzhouSoldierApplyExtraDamagePatch
{
    public static void Prefix(DamageResults results)
    {
        foreach (var extra in CaoArtOfWarSoldierDamageCascadePatch.Extras(results))
            extra.Receiver.LoseHpInternal(extra.UnblockedDamage);
    }
}
