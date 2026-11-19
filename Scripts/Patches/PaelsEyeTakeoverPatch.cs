using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Relics;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(PaelsEye), "AnyCardsPlayedThisTurn")]
public static class PaelsEyeTakeoverPatch
{
    public static void Postfix(PaelsEye __instance, ref bool __result)
    {
        if (!__result && AutoPlayRightToLeft.PlayedThisTurn(__instance.Owner))
            __result = true;
    }
}
