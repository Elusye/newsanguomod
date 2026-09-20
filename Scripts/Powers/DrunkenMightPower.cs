using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
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

    // 酒力上限：层数会被攻击加成、换大盏、胜利倍率等反复放大，逼近 int 上限时会在
    // 命令层的 (int) 转换处溢出，因此在此显式封顶（与 PowerModel.SetAmount 的内部钳制同值）。
    public const int MaxAmount = 999999999;

    // 获得酒力时封顶：把本次增量收缩到剩余空间内，保证总层数不超过 MaxAmount。
    // 只处理“已经挂在拥有者身上”的这份能力，因此多个玩家各自的酒力互不影响。
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (Owner is null || target != Owner || canonicalPower is not DrunkenMightPower)
        {
            return false;
        }
        decimal room = MaxAmount - Amount;
        if (amount <= room)
        {
            return false;
        }
        modifiedAmount = room;
        return true;
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

    // —— 酒力红温：层数越高，角色整体越红（100 层为最红）——

    // 达到最红所需的层数
    private const int MaxTintAmount = 100;

    // 最红时绿/蓝通道的倍率。Modulate 是乘法：只压 G/B、R 恒为 1，观感就是越喝越红。
    private const float MinGreenBlue = 0.35f;

    // 颜色过渡时长（秒）
    private const float TintDuration = 0.25f;

    private Tween? _tintTween;

    // 层数变化（首次获得、加层、减半）后刷新红温。
    // 该钩子会被全场任何能力的层数变化唤醒，因此只处理自己这一份。
    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
        {
            RefreshTint(Owner, Amount);
        }
        return Task.CompletedTask;
    }

    // 本能力被移除（层数归零 / 战斗结束清场）时恢复原色
    public override async Task AfterRemoved(Creature oldOwner)
    {
        await base.AfterRemoved(oldOwner);
        RefreshTint(oldOwner, 0);
    }

    // 按层数刷新角色染色；取不到角色节点（非战斗场景 / TestMode / 已离场）时静默跳过
    private void RefreshTint(Creature? creature, int amount)
    {
        if (creature is null)
        {
            return;
        }
        NCreatureVisuals? visuals = NCombatRoom.Instance?.GetCreatureNode(creature)?.Visuals;
        if (visuals is null || !GodotObject.IsInstanceValid(visuals))
        {
            return;
        }

        float greenBlue = 1f - (1f - MinGreenBlue) * Mathf.Clamp(amount / (float)MaxTintAmount, 0f, 1f);
        if (_tintTween is not null && _tintTween.IsValid())
        {
            _tintTween.Kill();
        }
        // 只补间 G/B 两个通道：不碰 R，也不碰 alpha（本体复活动画会在补间 modulate:a）
        _tintTween = visuals.CreateTween();
        _tintTween.TweenProperty(visuals, "modulate:g", greenBlue, TintDuration)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        _tintTween.TweenProperty(visuals, "modulate:b", greenBlue, TintDuration)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
    }
}
