using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “自刎归天”：本回合内每打出一张牌，就获得 Amount 层原版污染。
/// 由“自刎归天”附加，回合结束时自动移除。Amount 即每打出一张牌获得的污染层数。
/// 显示名/描述在 localization/*/powers.json 的 NEWSANGUO_POWER_BEAT_OF_DEATH_POWER.* 里；
/// 内部 id 与类名保持不变（存档兼容，且能力图标路径按类名取 res://…/powers/BeatOfDeathPower.png）。
/// </summary>
[RegisterPower]
public class BeatOfDeathPower : ModPowerTemplate
{
    /// <summary>
    /// 登记已打出、待结算的牌，及其打出瞬间的 Amount 快照（参考原版 OblivionPower）。
    /// 附加本能力的那张牌结算时，新建实例的字典仍为空，因此不会触发自己；
    /// 快照则保证结算用的是“打出瞬间”的数值（牌结算过程中 Amount 可能被叠加改变）。
    /// </summary>
    private class Data
    {
        public readonly Dictionary<CardModel, int> amountsForPlayedCards = new();
    }

    // 负面效果
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：计数器，Amount 表示每次出牌获得的污染层数
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 BeforeCardPlayed / AfterCardPlayed / AfterSideTurnEnd 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<TaintedPower>()];

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 打出前登记：登记自己打出的牌（任何类型，含打出瞬间的 Amount 快照）
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(cardPlay.Card, Amount);
        return Task.CompletedTask;
    }

    // 打出后核销并施加污染；附加此能力的牌本身不触发。
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 在册才触发，顺带清理字典（其他玩家的牌、附加本能力的牌不会登记）
        if (!GetInternalData<Data>().amountsForPlayedCards.Remove(cardPlay.Card, out int taintedAmount))
        {
            return;
        }
        if (!Owner.IsAlive || taintedAmount <= 0)
        {
            return;
        }

        await PowerCmd.Apply<TaintedPower>(choiceContext, Owner, taintedAmount, Owner, cardPlay.Card);
    }

    // 回合结束时移除本能力
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }
}
