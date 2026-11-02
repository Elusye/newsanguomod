using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SmartDescription), MethodType.Getter)]
public static class BewildermentSmartDescriptionPatch
{
    public static void Postfix(PowerModel __instance, LocString __result)
    {
        if (__instance is BewildermentPower power && power.IsMutable)
            __result.Add("OwnerName", power.Owner.Name);
    }
}
