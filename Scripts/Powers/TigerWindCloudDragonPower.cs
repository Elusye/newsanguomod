using System.Threading.Tasks;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “风从虎，云从龙”：层数即每当你打出一张龙虎牌时抽取的牌数。
/// </summary>
[RegisterPower]
public class TigerWindCloudDragonPower : ModPowerTemplate
{
    private sealed class Data
    {
        public readonly Dictionary<CardModel, int> AmountsForPlayedCards = new();
    }
    protected override object InitInternalData() => new Data();

    // 只使用出牌开始前已有的层数，刚获得或叠加的能力不追溯触发这次出牌。
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner.IsAlive && cardPlay.Card is NewsanguoCardTemplate { IsTigerDragonCard: true } card &&
            card.Owner.Creature == Owner)
            GetInternalData<Data>().AmountsForPlayedCards[card] = Amount;
        return Task.CompletedTask;
    }

    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示每次触发抽的牌数（每打出一次“风从虎，云从龙”叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要在牌被打出后被咨询
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 每当你打出一张龙虎牌，抽与层数等量的牌。
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        Player? player = card?.Owner;
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        if (Owner is null || !Owner.IsAlive || card is null || player is null || player.Creature != Owner)
        {
            return;
        }

        if (card is not NewsanguoCardTemplate { IsTigerDragonCard: true })
        {
            return;
        }

        if (!GetInternalData<Data>().AmountsForPlayedCards.Remove(card, out int amount) || amount <= 0)
            return;
        await CardPileCmd.Draw(choiceContext, amount, player);
    }
}
