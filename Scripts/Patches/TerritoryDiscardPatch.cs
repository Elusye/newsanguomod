using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Patches;

/// <summary>在实际弃牌命令入口排除地域牌，不影响消耗、生成或正常打出后的弃牌堆位置。</summary>
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.DiscardAndDraw))]
public static class TerritoryDiscardPatch
{
    public static void Prefix(ref IEnumerable<CardModel> cardsToDiscard) =>
        cardsToDiscard = cardsToDiscard.Where(card => !TerritoryCardTemplate.HasTerritoryKeyword(card)).ToList();
}

[HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHandForDiscard))]
public static class TerritoryDiscardSelectionPatch
{
    public static void Prefix(Player player, ref CardSelectorPrefs prefs, ref Func<CardModel, bool>? filter) =>
        Restrict(player, ref prefs, ref filter);

    internal static void Restrict(Player player, ref CardSelectorPrefs prefs, ref Func<CardModel, bool>? filter)
    {
        var originalFilter = filter;
        filter = card => !TerritoryCardTemplate.HasTerritoryKeyword(card) && (originalFilter?.Invoke(card) ?? true);
        int count = player.PlayerCombatState?.Hand.Cards.Count(filter) ?? 0;
        int min = Math.Min(prefs.MinSelect, count);
        int max = Math.Min(prefs.MaxSelect, count);
        prefs = new CardSelectorPrefs(prefs.Prompt, min, max)
        {
            RequireManualConfirmation = prefs.RequireManualConfirmation,
            Cancelable = prefs.Cancelable,
            Comparison = prefs.Comparison,
            UnpoweredPreviews = prefs.UnpoweredPreviews,
            PretendCardsCanBePlayed = prefs.PretendCardsCanBePlayed,
            ShouldGlowGold = prefs.ShouldGlowGold
        };
    }
}

/// <summary>兼容直接使用通用手牌选牌接口的弃牌效果。</summary>
[HarmonyPatch(typeof(CardSelectCmd), nameof(CardSelectCmd.FromHand))]
public static class TerritoryDiscardPromptPatch
{
    public static void Prefix(Player player, ref CardSelectorPrefs prefs, ref Func<CardModel, bool>? filter)
    {
        if (prefs.Prompt.LocTable == "card_selection" && prefs.Prompt.LocEntryKey == "TO_DISCARD")
            TerritoryDiscardSelectionPatch.Restrict(player, ref prefs, ref filter);
    }
}
