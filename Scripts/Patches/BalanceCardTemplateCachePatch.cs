using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Settings;

namespace newsanguo.Scripts.Patches;

// 图鉴可直接读取原型，也可直接克隆原型。规则切换时只清理原型缓存。
[HarmonyPatch]
public static class BalanceCardTemplateCachePatch
{
    private sealed class CacheRules
    {
        public bool? Enabled { get; set; }
    }

    private static readonly ConditionalWeakTable<CardModel, CacheRules> CachedRules = new();

    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.DynamicVars));
        yield return AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.EnergyCost));
        yield return AccessTools.PropertyGetter(typeof(CardModel), "LocalKeywords");
        yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.ToMutable));
        yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.MutableClone));
    }

    public static void Prefix(object __instance)
    {
        if (__instance is not CardModel { IsMutable: false } card ||
            card is not (BetterThanYilingFlames or LeaveItToFate or LetsGoDrinking or PartyOn or TigerWindCloudDragon))
        {
            return;
        }

        var cache = CachedRules.GetValue(card, static _ => new CacheRules());
        bool enabled = BalanceAdjustments.Enabled;
        if (cache.Enabled == enabled)
            return;

        cache.Enabled = enabled;
        DynamicVars(card) = null;
        Keywords(card) = null;
        EnergyCost(card) = null;
    }

    private static readonly AccessTools.FieldRef<CardModel, DynamicVarSet?> DynamicVars =
        AccessTools.FieldRefAccess<CardModel, DynamicVarSet?>("_dynamicVars");
    private static readonly AccessTools.FieldRef<CardModel, HashSet<CardKeyword>?> Keywords =
        AccessTools.FieldRefAccess<CardModel, HashSet<CardKeyword>?>("_keywords");
    private static readonly AccessTools.FieldRef<CardModel, CardEnergyCost?> EnergyCost =
        AccessTools.FieldRefAccess<CardModel, CardEnergyCost?>("_energyCost");
}
