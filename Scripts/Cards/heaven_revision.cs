using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class heaven_revision : NewsanguoCardTemplate
{
    // 基础耗能：2
    private const int energyCost = 2;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 2 点基础伤害（升级 5）；本场战斗中打出此牌前每失去一点天意之力额外造成 5（升级 8）点伤害；打出时失去 3（升级 2）点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(2m),
        new ExtraDamageVar(5m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) => CombatManager.Instance.History.Entries
                .OfType<PowerReceivedEntry>()
                .Where(entry => entry.Actor == card.Owner.Creature
                    && entry.Power is heavens_force
                    && entry.Amount < 0)
                .Sum(entry => -entry.Amount)),
        new PowerVar<heavens_force>("heavens_force", 3)
    ];

    // 悬停提示：展示“天意之力”与”天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay_power>()
    ];

    public heaven_revision() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is null || base.Owner is null)
        {
            return;
        }

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/heaven_revision");

        // 播放角色攻击动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Attack", base.Owner.Character.CastAnimDelay);

        // 造成计算伤害（基础 2 + 打出前累计失去天意之力点数 × 5）
        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 消耗天意之力（基础 3 点，升级后 2 点）
        await PowerCmd.Apply<heavens_force>(choiceContext, base.Owner.Creature, -DynamicVars["heavens_force"].IntValue, base.Owner.Creature, this, silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 基础伤害从 2 提升到 5，每点失去的天意之力额外伤害从 5 提升到 8，失去的天意之力 3 → 2
        base.DynamicVars.CalculationBase.UpgradeValueBy(3m);
        base.DynamicVars.ExtraDamage.UpgradeValueBy(3m);
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
    }
}
