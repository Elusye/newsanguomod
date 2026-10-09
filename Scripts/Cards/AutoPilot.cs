using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 「托管」（AutoPilot，蜀汉专属）：0 费罕见技能。
/// 获得 2 点能量（升级后 3 点）、抽 2 张牌（升级后 3 张），然后把本回合交给天意爷：
/// 这张牌结算完毕后，天意爷会从右到左自动打出你剩余的手牌（上限 13 张，见
/// <see cref="AutoPlayRightToLeft"/>，与「天意侵蚀」用的是同一个自动打牌器），然后结束你的回合。
///
/// 卡图 res://newsanguo/images/cards/AutoPilot.png；音效 event:/newsanguo/sfx/auto_pilot。
/// 卡面文案在 localization/*/cards.json 的 NEWSANGUO_CARD_AUTO_PILOT.*。
/// </summary>
[RegisterCard(typeof(ShuHanCardPool))]
public class AutoPilot : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：本回合获得的能量 2、抽牌数 2（升级后都变 3）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(2),
        new CardsVar(2)
    ];

    // 悬停提示：展示能量
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.ForEnergy(this)
    ];

    // 0 费、罕见、技能牌、目标为自己
    public AutoPilot() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑：先拿能量、再抽牌（抽到的牌随后会被天意爷打出去）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/auto_pilot");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得能量（本回合立即生效）
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, base.Owner);

        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, base.Owner);
    }

    /// <summary>
    /// 天意爷接管本回合：这张牌自己的结算（以及其它模型对这次出牌的反应）全部走完之后，
    /// 从右到左自动打出剩余手牌，然后结束你的回合。
    ///
    /// 为什么放在 Late 钩子里、而不是在 <see cref="OnPlay"/> 里直接自动打牌：
    /// 自动打牌会再走一遍完整的出牌流程，在卡牌自身还在结算时嵌套发起容易出问题；
    /// 这里等钩子触发（此时本牌已进 Play 堆、能量与抽牌都已结算完毕）再动手，
    /// 与「天意侵蚀」（<see cref="newsanguo.Scripts.Powers.HeavensDecayPower"/>）在钩子里
    /// 调同一个 <c>PlayHandRightToLeftAsync</c> 的写法一致。
    /// 结束回合的写法照抄「告老还乡」（Retire）与原版「虚空形态」（VoidForm）。
    /// </summary>
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 只有“这张牌自己被打出”才接管：别的牌结算时也会走到这里
        if (cardPlay.Card != this)
        {
            return;
        }

        // 战斗已结束/正在结束时不再接管
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return;
        }

        // 天意爷接管：从右到左自动打出剩余手牌（上限 13 张）
        await AutoPlayRightToLeft.PlayHandRightToLeftAsync(choiceContext, base.Owner);

        // 手牌打完后直接结束本回合（同「告老还乡」/原版「虚空形态」）
        if (!CombatManager.Instance.IsOverOrEnding)
        {
            PlayerCmd.EndTurn(base.Owner, false, null);
        }
    }

    // 升级后的效果逻辑：能量 2 → 3、抽牌 2 → 3
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
