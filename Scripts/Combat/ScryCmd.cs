using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace newsanguo.Scripts.Combat;

/// <summary>
/// 预见（Scry）机制：检视抽牌堆顶部的 X 张牌，可选择丢弃其中的任意张。
/// 预见不会导致洗牌；抽牌堆不足 X 张时展示全部剩余牌；
/// 卡牌按从顶部到底部的顺序（从左到右）展示。
/// </summary>
public static class ScryCmd
{
    /// <summary>
    /// 执行一次预见：展示抽牌堆顶部 <paramref name="amount"/> 张牌（不足则全部），
    /// 由玩家选择丢弃任意张（可不丢弃）。
    /// </summary>
    /// <param name="choiceContext">选择上下文。</param>
    /// <param name="player">执行预见的玩家。</param>
    /// <param name="amount">检视的卡牌数量。</param>
    public static async Task Scry(PlayerChoiceContext choiceContext, Player player, int amount)
    {
        if (amount <= 0 || player is null || CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        // 抽牌堆顶部到底部的顺序（index 0 为牌堆顶）
        CardPile drawPile = PileType.Draw.GetPile(player);
        List<CardModel> topCards = drawPile.Cards.Take(amount).ToList();
        if (topCards.Count == 0)
        {
            return;
        }

        // 最少选 0 张（可全不丢弃），最多选展示出的全部牌
        CardSelectorPrefs prefs = new(new LocString("cards", "NEWSANGUO_SCRY_PROMPT"), 0, topCards.Count);
        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(choiceContext, topCards, player, prefs);

        // 选中的牌移入弃牌堆（不触发洗牌，未选中的牌仍留在抽牌堆顶部）
        await CardPileCmd.Add(selected, PileType.Discard);
    }
}
