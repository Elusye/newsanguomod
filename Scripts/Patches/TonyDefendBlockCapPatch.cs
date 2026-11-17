using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Patches;
[HarmonyPatch(typeof(Creature), nameof(Creature.GainBlockInternal))]
public static class TonyDefendBlockCapPatch
{
    public static void Prefix(Creature __instance, ref decimal amount)
    {
        if (__instance.HasPower<TonyDefendPower>()) amount = Math.Min(amount, Math.Max(0m, 15m - __instance.Block));
    }
}
