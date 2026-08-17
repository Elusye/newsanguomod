using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace newsanguo.Scripts.Patches;

// “大都督到！”在选择消耗或变化牌时金色高亮。
//
// 实现原理：选择模式下，NHandCardHolder.ShouldGlowGold 会优先使用
// NPlayerHand.SelectModeGoldGlowOverride（即 CardSelectorPrefs.ShouldGlowGold），
// 完全替代卡牌自身的 ShouldGlowGoldInternal。因此无法靠卡牌自身实现，
// 需要在最终判定处（NHandCardHolder.ShouldGlowGold）用 Postfix 追加条件：
// 当正在进行“消耗/变化”类手牌选择，且该卡是“大都督到！”时，强制金色高亮。
public static class CommanderArrivesGlowState
{
    // 当前是否正处于“消耗/变化”类手牌选择中
    public static bool IsExhaustOrTransformSelection { get; private set; }

    // 由 NPlayerHand.SelectCards 前缀在每次选择开始时设置
    public static void SetFromSelection(CardSelectorPrefs prefs)
    {
        IsExhaustOrTransformSelection = IsExhaustOrTransformPrompt(prefs.Prompt);
    }

    // 选择结束后清除（由 NPlayerHand.AfterCardsSelected 后缀调用）
    public static void Clear()
    {
        IsExhaustOrTransformSelection = false;
    }

    // 判断选择提示是否为“消耗”或“变化”：
    //  - 原版：card_selection 表中的 TO_EXHAUST / TO_TRANSFORM
    //  - 本 mod：cards 表中的 NEWSANGUO_CARD_SELECT_ONE_TO_EXHAUST（叉出去！）、
    //    NEWSANGUO_CARD_SELECT_ANY（人体炼成术）
    private static bool IsExhaustOrTransformPrompt(LocString prompt)
    {
        if (prompt.LocTable == "card_selection")
        {
            return prompt.LocEntryKey is "TO_EXHAUST" or "TO_TRANSFORM";
        }
        if (prompt.LocTable == "cards")
        {
            return prompt.LocEntryKey is "NEWSANGUO_CARD_SELECT_ONE_TO_EXHAUST" or "NEWSANGUO_CARD_SELECT_ANY";
        }
        return false;
    }
}

// 记录当前选择是否为“消耗/变化”选择（前缀在每次选择开始时执行）
[HarmonyPatch(typeof(NPlayerHand), nameof(NPlayerHand.SelectCards))]
public static class CommanderArrivesSelectCardsPatch
{
    public static void Prefix(CardSelectorPrefs prefs)
    {
        CommanderArrivesGlowState.SetFromSelection(prefs);
    }
}

// 选择结束（含取消）时清除状态
[HarmonyPatch(typeof(NPlayerHand), "AfterCardsSelected")]
public static class CommanderArrivesSelectionEndPatch
{
    public static void Postfix()
    {
        CommanderArrivesGlowState.Clear();
    }
}

// “大都督到！”在手牌选择期间金色高亮
[HarmonyPatch(typeof(NHandCardHolder), "ShouldGlowGold", MethodType.Getter)]
public static class CommanderArrivesGlowPatch
{
    public static void Postfix(NHandCardHolder __instance, ref bool __result)
    {
        if (__result || !CommanderArrivesGlowState.IsExhaustOrTransformSelection)
        {
            return;
        }
        if (__instance.CardNode?.Model is commander_arrives)
        {
            __result = true;
        }
    }
}
