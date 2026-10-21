using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Interop;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

// 原版 SmartDescription 独立创建 LocString，不会调用各能力的 Description。
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SmartDescription), MethodType.Getter)]
public static class SunqianVigorSmartDescriptionPatch
{
    public static void Postfix(PowerModel __instance, LocString __result)
    {
        if (__instance is ToABiggerGobletPower or WheresWinePower or FeelNoAcidPower
            or SovereignFormPower or TriumphBrewPower)
        {
            SunqianUniverseVigorInterop.AddActiveResourceName(__result, __instance);
        }
    }
}
