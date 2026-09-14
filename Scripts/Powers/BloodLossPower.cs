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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “自刎”：本回合内每打出一张攻击牌，就对自己造成 Amount 点伤害（不受力量等伤害修饰）。
/// 打出“自刎归天”后附加，回合结束时自动移除。Amount 即每张攻击牌的自伤数值。
/// </summary>
[RegisterPower]
public class BloodLossPower : ModPowerTemplate
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

    // 本次结算的自刎伤害是否正由奥斯提承担（仅在 Damage 调用期间为 true）
    private bool ostyIsTakingThisDamage;

    // 负面效果
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：计数器，Amount 表示“每打出一张攻击牌对自己造成的伤害”
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 BeforeCardPlayed / AfterCardPlayed / AfterSideTurnEnd 不会被调用
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

    // 打出前登记：只登记自己打出的攻击牌（含打出瞬间的 Amount 快照）
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
        {
            return Task.CompletedTask;
        }
        if (cardPlay.Card.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(cardPlay.Card, Amount);
        return Task.CompletedTask;
    }

    // 打出后核销并结算：对自己造成 hpCost 点伤害（不可受力量等修饰，但仍可被格挡）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 在册才触发，顺带清理字典（未登记的牌：非攻击牌、其他玩家的牌、附加本能力的牌）
        if (!GetInternalData<Data>().amountsForPlayedCards.Remove(cardPlay.Card, out int hpCost))
        {
            return;
        }
        if (!Owner.IsAlive || hpCost <= 0)
        {
            return;
        }

        // 自刎伤害触发音效（对应 FMOD 事件 event:/newsanguo/sfx/blood_loss）
        NewsanguoSfx.Play("event:/newsanguo/sfx/blood_loss");

        // ValueProp.Unpowered：来自能力造成的伤害，不受力量等伤害修饰（仍可被格挡）。
        // 若场上有存活的奥斯提，则由它承担这次自刎伤害（见 ModifyUnblockedDamageTarget）。
        ostyIsTakingThisDamage = true;
        try
        {
            await CreatureCmd.Damage(choiceContext, Owner, hpCost, ValueProp.Unpowered, dealer: Owner, cardSource: null, cardPlay: null);
        }
        finally
        {
            ostyIsTakingThisDamage = false;
        }
    }

    /// <summary>
    /// 把这次自刎的未被格挡伤害转给奥斯提承担。走引擎自带的伤害转移通道，
    /// 因此格挡、溢出伤害（奥斯提被打死时多出的部分回到你身上）、死亡结算都与原版奥斯提“替你去死”一致。
    /// </summary>
    public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature? dealer)
    {
        if (!ostyIsTakingThisDamage || target != Owner)
        {
            return target;
        }
        Player? player = Owner.Player;
        if (player is null || !player.IsOstyAlive)
        {
            return target;
        }
        return player.Osty!;
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
