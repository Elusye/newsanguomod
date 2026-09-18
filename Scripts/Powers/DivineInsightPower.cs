using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
using newsanguo.Scripts.Combat;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “参悟天意”：你每打出一张牌，获得与层数相等的天意之力。可叠加。
/// 参照原版“残影”（AfterimagePower）：打牌开始前记账层数，打出后按键结账并移除。
/// 因此“打牌开始时本能力还不存在”的那张牌（即首次打出“参悟天意”自身）不会触发。
/// </summary>
[RegisterPower]
public class DivineInsightPower : ModPowerTemplate
{
    // 账本：牌 → 那张牌开始打出时的层数。
    // 用 CardModel 作键（引用相等），精确到“这一张牌的这一次出牌”。
    private class Data
    {
        public readonly Dictionary<CardModel, int> amountsForPlayedCards = new();
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，层数 = 每打出一张牌获得的天意之力（多次打出叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 出牌钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 悬停提示：说明文本中会提到“天意之力”，与“天意侵蚀”成对展示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 打牌开始前：记录此刻的层数
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        if (Owner is null || cardPlay.Card?.Owner?.Creature != Owner)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(cardPlay.Card, Amount);
        return Task.CompletedTask;
    }

    // 打出任意牌后：按记账的层数获得天意之力（账本里没有这张牌则不触发，每张牌只结算一次）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is null)
        {
            return;
        }
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        if (cardPlay.Card?.Owner?.Creature != Owner)
        {
            return;
        }
        if (!GetInternalData<Data>().amountsForPlayedCards.Remove(cardPlay.Card, out int amount) || amount <= 0)
        {
            return;
        }

        // 触发“参悟天意”音效（对应 FMOD 事件 event:/newsanguo/sfx/divine_insight_power）
        NewsanguoSfx.Play("event:/newsanguo/sfx/divine_insight_power");

        await HeavensForce.Add(choiceContext, Owner.Player, amount, cardPlay.Card);
    }
}
