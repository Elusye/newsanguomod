using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Combat;

/// <summary>通过原版动作队列同步地域牌换位，禁止只改本地显示顺序。</summary>
public static class TerritoryHandOrder
{
    internal static bool CanReposition(CardModel card) => card is not InvincibleWithinThreeHundredLi ||
        (card.CombatState is { } combat && !CombatManager.Instance.History.CardPlaysStarted.Any(entry =>
            entry.CardPlay.Player == card.Owner && !entry.CardPlay.IsAutoPlay && entry.HappenedThisTurn(combat)));

    internal static void ApplyMove(CardModel card, CardPile hand, int index)
    {
        // 原版内部列表保留实体顺序，不模拟离手/入手，以免触发抽牌及弃牌效果。
        var cards = (System.Collections.Generic.IList<CardModel>)hand.Cards;
        int currentIndex = cards.IndexOf(card);
        int targetIndex = Math.Clamp(index, 0, cards.Count - 1);
        // 队列执行时检查手动出牌历史，避免本地预检与实际执行之间状态变化。
        if (targetIndex == currentIndex || !CanReposition(card))
            return;
        cards.Remove(card);
        cards.Insert(targetIndex, card);
        hand.InvokeContentsChanged();
        var ui = NPlayerHand.Instance;
        if (ui is not null && LocalContext.IsMe(card.Owner))
        {
            var holders = ui.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToList();
            int position = 0;
            foreach (var model in hand.Cards)
            {
                var holder = holders.FirstOrDefault(h => h.CardNode?.Model == model);
                if (holder is not null) ui.CardHolderContainer.MoveChild(holder, position++);
            }
            ui.ForceRefreshCardIndices();
        }
        foreach (var model in hand.Cards) model.BaseReplayCount = model.BaseReplayCount;
    }

    public static void Request(CardModel card, int index) => TerritoryHandAction.Request(card, index);
}
