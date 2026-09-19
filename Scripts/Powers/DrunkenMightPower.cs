using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “酒力”：增加攻击牌造成的伤害（层数 = 加成数值），并在打出攻击牌后减半。
/// 参照原版“残影”（AfterimagePower）：打牌开始前记账层数，打出后按键结账并移除，
/// 用打出瞬间的层数结算减半，因此“打牌开始时本能力还不存在”的那张牌不会触发。
/// </summary>
// 注册能力到游戏
[RegisterPower]
public class DrunkenMightPower : ModPowerTemplate
{
    // 账本：牌 → 那张牌开始打出时的层数。
    // 用 CardModel 作键（引用相等），精确到“这一张牌的这一次出牌”。
    private class Data
    {
        public readonly Dictionary<CardModel, int> amountsForPlayedCards = new();
    }

    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示层数（和力量一致）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 酒力不会为负数
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 BeforeCardPlayed / AfterCardPlayed 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 增加攻击牌造成的伤害（返回要叠加的数值增量）
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 只有这张能力的拥有者打出的攻击牌才享受加成
        if (base.Owner != dealer)
        {
            return 0m;
        }
        if (!props.IsPoweredAttack())
        {
            return 0m;
        }
        return base.Amount;
    }

    // 打牌开始前：记录此刻的层数（只登记本能力拥有者打出的攻击牌）
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        if (Owner is null || card is null || card.Owner?.Creature != Owner)
        {
            return Task.CompletedTask;
        }
        if (card.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(card, Amount);
        return Task.CompletedTask;
    }

    // 打出攻击牌后：按记账的层数减半（账本里没有这张牌则不触发，每张牌只结算一次）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        if (Owner is null || card is null)
        {
            return;
        }
        if (!GetInternalData<Data>().amountsForPlayedCards.Remove(card, out int amount))
        {
            return;
        }

        // 「杯酒斩击」的酒力处理（先翻倍再减半）在其自身 OnPlay 内手动完成，此处跳过一次，避免重复减半
        if (card is WineCut)
        {
            return;
        }

        // 按打出瞬间的层数减半（向下取整）
        await SetAmount(choiceContext, amount / 2, card);
    }

    // 酒力减半（向下取整）：打出攻击牌后消耗一半酒力，供「杯酒斩击」在自身 OnPlay 内手动调用
    public async Task HalfForCard(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        await SetAmount(choiceContext, Amount / 2, cardSource);
    }

    // 把酒力调整为指定值
    private async Task SetAmount(PlayerChoiceContext choiceContext, int target, CardModel? cardSource)
    {
        if (Owner is null || target == Amount)
        {
            return;
        }
        await PowerCmd.ModifyAmount(
            choiceContext,
            this,
            target - Amount,
            Owner,
            cardSource,
            silent: false);
    }
}
