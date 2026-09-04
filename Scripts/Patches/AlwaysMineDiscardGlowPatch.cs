using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace newsanguo.Scripts.Patches;

// “原本就是我的！”（带“奇巧”）在被要求弃手牌时金色高亮。
//
// 实现原理与 CommanderArrivesGlowPatch 相同：选择模式下 NHandCardHolder.ShouldGlowGold
// 由 NPlayerHand.SelectModeGoldGlowOverride（即 CardSelectorPrefs.ShouldGlowGold）接管，
// 无法靠卡牌自身实现，因此在最终判定处（ShouldGlowGold）用 Postfix 追加条件：
// 当正在进行“从手牌挑选弃牌”的选择，且该卡是“原本就是我的！”时，强制金色高亮，
// 提示玩家它带“奇巧”，在弃牌类选择中有特殊互动。
public static class AlwaysMineDiscardGlowState
{
    // 当前是否正处于“挑选要丢弃的手牌”选择中
    public static bool IsDiscardSelection { get; private set; }

    // 由 NPlayerHand.SelectCards 前缀在每次选择开始时设置
    public static void SetFromSelection(CardSelectorPrefs prefs)
    {
        IsDiscardSelection = IsDiscardPrompt(prefs.Prompt);
    }

    // 选择结束后清除（由 NPlayerHand.AfterCardsSelected 后缀调用）
    public static void Clear()
    {
        IsDiscardSelection = false;
    }

    // 判断选择提示是否为“弃牌”：
    //  - 原版：card_selection 表中的 TO_DISCARD（如“咱们好好谈谈！”选择丢弃）
    private static bool IsDiscardPrompt(LocString prompt)
    {
        return prompt.LocTable == "card_selection" && prompt.LocEntryKey is "TO_DISCARD";
    }
}

// 记录当前选择是否为“弃牌”选择（前缀在每次选择开始时执行）
[HarmonyPatch(typeof(NPlayerHand), nameof(NPlayerHand.SelectCards))]
public static class AlwaysMineDiscardSelectPatch
{
    public static void Prefix(CardSelectorPrefs prefs)
    {
        AlwaysMineDiscardGlowState.SetFromSelection(prefs);
    }
}

// 选择结束（含取消）时清除状态
[HarmonyPatch(typeof(NPlayerHand), "AfterCardsSelected")]
public static class AlwaysMineDiscardSelectionEndPatch
{
    public static void Postfix()
    {
        AlwaysMineDiscardGlowState.Clear();
    }
}

// “原本就是我的！”在弃牌选择期间金色高亮
[HarmonyPatch(typeof(NHandCardHolder), "ShouldGlowGold", MethodType.Getter)]
public static class AlwaysMineDiscardGlowPatch
{
    public static void Postfix(NHandCardHolder __instance, ref bool __result)
    {
        if (__result || !AlwaysMineDiscardGlowState.IsDiscardSelection)
        {
            return;
        }
        if (__instance.CardNode?.Model is always_mine)
        {
            __result = true;
        }
    }
}
