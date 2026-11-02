using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Patches;

/// <summary>仅更改提示文本，不改变能力的类型、层数或持续时间。</summary>
public static class ReversalPresentation
{
    internal static bool IsActive(PowerModel power) => power.IsMutable &&
        power.Owner.HasPower<ReversalPower>() && !AbilitySuppression.HoldsHanzhong(power.Owner);

    internal static string? Kind(PowerModel power) => power switch
    {
        WeakPower => "WEAK",
        VulnerablePower => "VULNERABLE",
        FrailPower => "FRAIL",
        _ => null
    };

    internal static decimal Percent(PowerModel power)
    {
        // 读取已经过实际反转补丁处理的倍率，兼容原版对虚弱/易伤倍率的修正。
        decimal multiplier = power switch
        {
            WeakPower weak => weak.ModifyDamageMultiplicative(null, 0m, ValueProp.Move, power.Owner, null),
            VulnerablePower vulnerable => vulnerable.ModifyDamageMultiplicative(power.Owner, 0m, ValueProp.Move, null, null),
            FrailPower frail => frail.ModifyBlockMultiplicative(power.Owner, 0m, ValueProp.Move, null, null),
            _ => 1m
        };
        return Math.Round((power is VulnerablePower ? 1m - multiplier : multiplier - 1m) * 100m, 2);
    }

    internal static void AddStatus(PowerModel power, LocString description)
    {
        if (!power.IsMutable)
        {
            description.Add("ReversalStatus", string.Empty);
            return;
        }
        if (AbilitySuppression.HoldsHanzhong(power.Owner))
        {
            description.Add("ReversalStatus", new LocString("powers", "NEWSANGUO_REVERSAL_STATUS.paused").GetFormattedText());
            return;
        }
        var names = power.Owner.Powers.Where(p => p.Amount > 0 && Kind(p) is not null)
            .Select(p => new LocString("powers", Kind(p) + "_POWER.title").GetFormattedText())
            .Distinct().ToArray();
        if (names.Length == 0)
        {
            description.Add("ReversalStatus", new LocString("powers", "NEWSANGUO_REVERSAL_STATUS.none").GetFormattedText());
            return;
        }
        var status = new LocString("powers", "NEWSANGUO_REVERSAL_STATUS.active");
        string separator = new LocString("powers", "NEWSANGUO_REVERSAL_STATUS.separator").GetFormattedText();
        status.Add("Powers", string.Join(separator, names));
        description.Add("ReversalStatus", status.GetFormattedText());
    }
}

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.Title), MethodType.Getter)]
public static class ReversalPowerTitlePatch
{
    public static void Postfix(PowerModel __instance, ref LocString __result)
    {
        if (ReversalPresentation.Kind(__instance) is { } kind && ReversalPresentation.IsActive(__instance))
            __result = new LocString("powers", "NEWSANGUO_REVERSAL_" + kind + ".title");
    }
}

[HarmonyPatch]
public static class ReversalPowerDescriptionPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(typeof(PowerModel), nameof(PowerModel.Description));
        yield return AccessTools.PropertyGetter(typeof(PowerModel), nameof(PowerModel.SmartDescription));
        yield return AccessTools.PropertyGetter(typeof(PowerModel), nameof(PowerModel.RemoteDescription));
    }
    public static void Postfix(PowerModel __instance, MethodBase __originalMethod, ref LocString __result)
    {
        if (__instance is ReversalPower)
        {
            ReversalPresentation.AddStatus(__instance, __result);
            return;
        }
        if (ReversalPresentation.Kind(__instance) is not { } kind || !ReversalPresentation.IsActive(__instance)) return;
        string suffix = __originalMethod.Name == "get_Description" ? "description" : "smartDescription";
        var description = new LocString("powers", "NEWSANGUO_REVERSAL_" + kind + "." + suffix);
        description.Add("Amount", __instance.Amount);
        description.Add("Percent", ReversalPresentation.Percent(__instance));
        __result = description;
    }
}
