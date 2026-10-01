using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HeavenRevision : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌数值：**没有基础伤害**（CalculationBase = 0），伤害完全来自“本场战斗中已获得的天意之力”：
    // CalculatedDamage = CalculationBase + ExtraDamage × 倍数（倍数 = 本场战斗累计获得的天意之力）
    //  → 未升级 2 倍、升级后 3 倍（ExtraDamage 2 → 3，见 OnUpgrade）
    // 纯动态伤害牌照原版 CalculatedDamageVar 的说明写：CalculationBase 0 + ExtraDamage，
    // 参考原版「全身撞击」BodySlam（CalculatedDamageVar.cs:24-25）。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(0m),
        new ExtraDamageVar(2m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) => HeavensForce.GainedThisCombat(card.Owner))
    ];

    // 悬停提示：展示“天意之力”与”天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public HeavenRevision() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/heaven_revision");

        // 造成计算伤害（本场战斗中已获得的天意之力 × 2，升级后 × 3；无基础伤害）
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>
    /// 触发条件（两者任一满足即回手，从弃牌堆/抽牌堆/消耗堆等位置回收，重新可以再打一次）：
    ///   1) 每当你进入额外回合时（<see cref="AfterTakingExtraTurn"/>）；
    ///   2) 每当你生成牌时（<see cref="AfterCardGeneratedForCombat"/>，2026-10-01 追加）。
    /// 写法照抄原版「就这么办」（MakeItSo.cs:35-47）：条件满足就把自己放回手牌
    /// （CardPileCmd.Add(this, PileType.Hand)），并先判断当前是否已在手牌（避免重复放入）。
    ///
    /// 多人注意：这两个钩子都由引擎在两端**对称**分发（生成牌的 creator 来自动作本身），
    /// 且只用同步状态判断，因此两端结果一致；这里**不能**加 LocalContext.IsMe 之类的本机判定。
    /// </summary>
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player != base.Owner)
        {
            return;
        }
        if (Pile?.Type == PileType.Hand)
        {
            return;
        }

        await CardPileCmd.Add(this, PileType.Hand);
    }

    /// <summary>
    /// 每当你**生成牌**时同样回手（与 <see cref="AfterTakingExtraTurn"/> 同一套写法与判定）。
    /// 只认 <paramref name="creator"/> == 自己：别人生成的牌不触发；生成的就是这张牌本身时也不再处理。
    /// </summary>
    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (creator != base.Owner)
        {
            return;
        }
        if (card == this)
        {
            return;
        }
        if (Pile?.Type == PileType.Hand)
        {
            return;
        }

        await CardPileCmd.Add(this, PileType.Hand);
    }

    // 升级后的效果逻辑：每点获得的天意之力所对应的伤害 2 → 3（仍然没有基础伤害）
    protected override void OnUpgrade()
    {
        base.DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
