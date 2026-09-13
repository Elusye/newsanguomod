using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “曹氏兵法”赋予的可叠加能力：层数（Amount）= 已打出的“曹氏兵法”张数。
/// 1) 你每打出一张牌，就召唤与层数相同数量的奥斯蒂生命值（OstyCmd.Summon，同原版“吸引仇恨”PullAggro）；
/// 2) 每当任何生物死亡，你获得与层数相同的力量（触发点同原版“忧郁”Melancholy 的 AfterDeath Hook）。
/// 参照原版“残影”（AfterimagePower）：打牌开始前记账层数，打出后按键结账并移除。
/// 因此“打牌开始时本能力还不存在”的那张牌（即首次打出“曹氏兵法”自身）不会触发；
/// 已有该能力后再打出“曹氏兵法”属于正常出牌，会照常触发。
/// </summary>
[RegisterPower]
public class CaosArtOfWarPower : ModPowerTemplate
{
    // 账本：牌 → 那张牌开始打出时的层数。
    // 用 CardModel 作键（引用相等），精确到“这一张牌的这一次出牌”。
    private class Data
    {
        public readonly Dictionary<CardModel, int> amountsForPlayedCards = new();
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 可叠加：Counter 显示当前层数（= 已打出的“曹氏兵法”张数），每层提供相同的触发强度
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 出牌与死亡钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 能力图标资源（Counter 层数会显示在图标角标上）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 打牌开始前：记录此刻的层数
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        if (Owner is null || card is null || card.Owner?.Creature != Owner)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(card, Amount);
        return Task.CompletedTask;
    }

    // 每当你打出一张牌时：按记账的层数召唤奥斯蒂生命值（账本里没有这张牌则不触发，每张牌只结算一次）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        Player? player = card?.Owner;
        if (Owner is null || !Owner.IsAlive || card is null || player is null || player.Creature != Owner)
        {
            return;
        }
        if (!GetInternalData<Data>().amountsForPlayedCards.Remove(card, out int amount) || amount <= 0)
        {
            return;
        }

        Flash();
        await OstyCmd.Summon(choiceContext, player, amount, card);
    }

    // 每当任何生物死亡时：获得与层数相同的力量
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner is null || !Owner.IsAlive || Amount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
