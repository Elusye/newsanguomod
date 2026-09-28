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
    /// 每当你进入额外回合时，将这张牌放回手牌（从弃牌堆等位置回收，重新可以再打一次）。
    /// 照抄原版「就这么办」（MakeItSo.cs:35-47）的写法：用 AfterTakingExtraTurn / 同类的
    /// “条件满足就把自己放回手牌”钩子 + CardPileCmd.Add(this, PileType.Hand)，
    /// 并先判断当前是否已在手牌（避免重复放入）。
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

    // 升级后的效果逻辑：每点获得的天意之力所对应的伤害 2 → 3（仍然没有基础伤害）
    protected override void OnUpgrade()
    {
        base.DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}
