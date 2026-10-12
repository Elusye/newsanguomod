using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

/// <summary>每次生成界铁骑智能描述时，读取敌人当前实际被禁用的正面能力名称。</summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SmartDescription), MethodType.Getter)]
public static class IronCavalrySmartDescriptionPatch
{
    public static void Postfix(PowerModel __instance, LocString __result)
    {
        if (__instance is not IronCavalryPower iron || !iron.IsMutable)
            return;
        string[] names = iron.Amount > 0 && iron.Owner.Side == CombatSide.Enemy
            ? iron.Owner.Powers.Where(IronCavalryBuffSuppressionPatch.CanSuppress)
                .Select(power => power.Title.GetFormattedText()).Distinct().ToArray()
            : System.Array.Empty<string>();
        string separator = new LocString("powers", "NEWSANGUO_POWER_IRON_CAVALRY_POWER.listSeparator").GetFormattedText();
        string text = names.Length > 0 ? string.Join(separator, names)
            : new LocString("powers", "NEWSANGUO_POWER_IRON_CAVALRY_POWER.noSuppressedPowers").GetFormattedText();
        __result.Add("SuppressedPowers", text);
    }
}