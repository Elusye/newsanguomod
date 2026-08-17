using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
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
/// “自刎”：本回合内每打出一张攻击牌，就失去 3 点生命。
/// 打出“自刎归天”后附加，回合结束时自动移除。
/// </summary>
[RegisterPower]
public class blood_loss : ModPowerTemplate
{
    // 每打出一张攻击牌失去的生命（初始为 0，由“自刎归天”打出时通过 AddHpCost 累加设定）
    private int hpCostPerCard = 0;

    // 每次打出“自刎归天”叠加的掉血数值（可多次叠加，同一实例内累加）
    public void AddHpCost(int cost)
    {
        hpCostPerCard += cost;
        // 直接修改字段绕过了 ModifyAmount，需主动通知 UI 刷新图标层数（DisplayAmount）
        InvokeDisplayAmountChanged();
    }

    // 记录附加本能力的卡牌，打出该牌本身不触发掉血
    private class Data
    {
        public CardModel? sourceCard;
    }

    // 负面效果
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：计数器，Amount 表示剩余回合数，回合结束时 -1 归零自动移除
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 AfterCardPlayed / AfterSideTurnEnd 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 图标上显示每次打牌失去的生命（而非剩余回合数）
    public override int DisplayAmount => hpCostPerCard;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 记录附加本能力的卡牌（由“自刎归天”在打出时设置）
    public void SetSourceCard(CardModel card)
    {
        GetInternalData<Data>().sourceCard = card;
    }

    // 每打出一张攻击牌（附加本能力的卡牌本身除外），失去 hpCostPerCard 点生命（不可格挡、不受力量等伤害修饰）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !Owner.IsAlive)
        {
            return;
        }

        // 只有攻击牌触发掉血
        if (cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        // 打出“自刎归天”本身不触发掉血
        if (cardPlay.Card == GetInternalData<Data>().sourceCard)
        {
            return;
        }

        // 自刎掉血触发音效（对应 FMOD 事件 event:/newsanguo/sfx/blood_loss）
        SfxCmd.Play("event:/newsanguo/sfx/blood_loss");

        await CreatureCmd.Damage(choiceContext, Owner, hpCostPerCard, ValueProp.Unblockable | ValueProp.Unpowered, dealer: null, cardSource: null, cardPlay: cardPlay);
    }

    // 回合结束时移除本能力
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
