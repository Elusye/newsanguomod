using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Patches;
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SmartDescription), MethodType.Getter)]
public static class IronCavalrySmartDescriptionPatch
{
    public static void Postfix(PowerModel __instance, LocString __result)
    {
        if (__instance is not IronCavalryPower iron || !iron.IsMutable) return;
        var names = iron.Owner.Powers.Where(IronCavalryPower.CanSuppressPositivePower)
            .Select(power => power.Title.GetFormattedText()).Distinct().ToArray();
        string separator = new LocString("powers", "NEWSANGUO_POWER_IRON_CAVALRY_POWER.listSeparator").GetFormattedText();
        __result.Add("SuppressedPowers", names.Length == 0
            ? new LocString("powers", "NEWSANGUO_POWER_IRON_CAVALRY_POWER.noSuppressedPowers").GetFormattedText()
            : string.Join(separator, names));
    }
}
