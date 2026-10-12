using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Patches;

/// <summary>仅反转这三种能力自身的乘法修正，正常倍率为1时保持不变。</summary>
public static class FireWaterHarmonyReversal
{
    public static void Apply(PowerModel power, ref decimal multiplier)
    {
        if (power.Owner.HasPower<ReversalPower>())
            multiplier = Math.Max(0m, 2m - multiplier);
    }
}
[HarmonyPatch(typeof(WeakPower), nameof(WeakPower.ModifyDamageMultiplicative))]
public static class FireWaterHarmonyWeakPatch
{
    public static void Postfix(WeakPower __instance, ref decimal __result) => FireWaterHarmonyReversal.Apply(__instance, ref __result);
}
[HarmonyPatch(typeof(VulnerablePower), nameof(VulnerablePower.ModifyDamageMultiplicative))]
public static class FireWaterHarmonyVulnerablePatch
{
    public static void Postfix(VulnerablePower __instance, ref decimal __result) => FireWaterHarmonyReversal.Apply(__instance, ref __result);
}
[HarmonyPatch(typeof(FrailPower), nameof(FrailPower.ModifyBlockMultiplicative))]
public static class FireWaterHarmonyFrailPatch
{
    public static void Postfix(FrailPower __instance, ref decimal __result) => FireWaterHarmonyReversal.Apply(__instance, ref __result);
}
