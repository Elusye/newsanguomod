using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// 「直奔诸葛亮四轮车！」附带的回合内能力，做两件事：
/// 1) 本回合内你的攻击牌费用为 0（含之后才抽到的攻击牌 —— 走引擎的改费钩子，不逐张改牌面费用）；
/// 2) 你使用攻击牌击杀敌人时，每击杀 1 个敌人获得 Amount 张「四轮车」（AoE 多杀多给）。
/// 回合结束时自动移除。显示名/描述在 localization/*/powers.json 的
/// NEWSANGUO_POWER_CHARGE_TO_ZHUGE_LIANGS_CART_POWER.* 里；
/// 内部 id 与类名保持不变（能力图标路径按类名取 res://…/powers/ChargeToZhugeLiangsCartPower.png）。
/// </summary>
[RegisterPower]
public class ChargeToZhugeLiangsCartPower : ModPowerTemplate
{
    /// <summary>
    /// 打出攻击牌瞬间的“存活敌人快照”，牌结算完后用它做差集，数出这张牌打死了几个敌人。
    /// 按 <see cref="CardPlay"/> 分别记录（牌可以嵌套打出别的牌，不能只用一个字段）。
    /// </summary>
    private class Data
    {
        public readonly Dictionary<CardPlay, List<Creature>> enemiesBeforePlay = new();
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示“每次击杀获得几张四轮车”（同一回合多次打出本牌会叠倍率）
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

    /// <summary>
    /// 本回合内，自己所属的攻击牌费用一律为 0。
    /// X 费牌的费用是负数（-1），引擎在 <c>Hook.ModifyEnergyCostInCombat</c> 里遇到
    /// <c>originalCost &lt; 0</c> 就直接返回、根本不会走到这里，所以无需额外判 CostsX。
    /// </summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Type != CardType.Attack || card.Owner?.Creature != Owner)
        {
            return false;
        }

        modifiedCost = 0m;
        return true;
    }

    // 打出攻击牌前：记录此刻存活的敌人（非自己打出的攻击牌不登记）
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Attack || cardPlay.Card.Owner?.Creature != Owner)
        {
            return Task.CompletedTask;
        }

        if (Owner.CombatState is not { } combatState)
        {
            return Task.CompletedTask;
        }

        GetInternalData<Data>().enemiesBeforePlay[cardPlay] =
            combatState.GetOpponentsOf(Owner).Where(creature => creature.IsAlive).ToList();
        return Task.CompletedTask;
    }

    // 攻击牌结算完：数出快照里已经死掉的敌人数量，按数量给「四轮车」
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 在册才触发，顺带清理字典（未登记的牌：非攻击牌、其他玩家的牌）
        if (!GetInternalData<Data>().enemiesBeforePlay.Remove(cardPlay, out List<Creature>? enemiesBefore))
        {
            return;
        }

        int kills = enemiesBefore.Count(creature => !creature.IsAlive);
        if (kills <= 0 || Owner.Player is not { } player)
        {
            return;
        }

        // 战斗正在收尾（这一下已经打完最后一只怪）时不再发牌
        if (CombatManager.Instance.IsOverOrEnding || Owner.CombatState is not { } combatState)
        {
            return;
        }

        int cartCount = kills * Amount;
        for (int i = 0; i < cartCount; i++)
        {
            CardModel cart = combatState.CreateCard<FourWheeledCart>(player);
            await CardPileCmd.AddGeneratedCardToCombat(cart, PileType.Hand, player);
        }

        Flash();
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
