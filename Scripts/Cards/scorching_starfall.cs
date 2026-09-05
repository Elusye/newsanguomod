using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class scorching_starfall : NewsanguoCardTemplate
{
    // 基础耗能
    private const int energyCost = 4;
    // 卡牌类型
    private const CardType type = CardType.Attack;
    // 卡牌稀有度
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：所有敌人
    private const TargetType targetType = TargetType.AllEnemies;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每 3（升级 2）点酒力触发一段全体伤害 2（升级 3）；失去 5（升级 4）点天意之力；
    // 战斗中动态计算本次的攻击段数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(2, ValueProp.Move),
        new IntVar("wine_threshold", 3),
        new PowerVar<heavens_force>("heavens_force", 5),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits").WithMultiplier(static (card, _) =>
        {
            decimal wine = card.Owner?.Creature.GetPower<drunken_might>()?.Amount ?? 0m;
            int per = card is scorching_starfall s ? s.DynamicVars["wine_threshold"].IntValue : 3;
            return per > 0 ? Math.Floor(wine / per) : 0m;
        })
    ];

    // 鼠标悬停时显示“酒力”“天意之力”与“天意侵蚀”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<drunken_might>(),
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay_power>()
    ];

    public scorching_starfall() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/scorching_starfall");

        Player? owner = base.Owner;
        ICombatState? combatState = base.CombatState;
        if (owner is null || combatState is null)
        {
            return;
        }

        // 获取当前酒力层数（打出时、失去天意之前的快照，决定攻击段数）
        int wineAmount = owner.Creature.GetPower<drunken_might>()?.Amount ?? 0;
        int threshold = DynamicVars["wine_threshold"].IntValue;
        int hits = threshold > 0 ? wineAmount / threshold : 0;

        // 每段酒力阈值触发一次全体伤害（每段伤害不受酒力加伤影响的部分由各段结算）
        if (hits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.IntValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(combatState)
                .WithHitCount(hits)
                .Execute(choiceContext);
        }

        // 失去 5 点天意之力（升级后 4 点）
        await PowerCmd.Apply<heavens_force>(
            choiceContext,
            owner.Creature,
            -DynamicVars["heavens_force"].IntValue,
            owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 每段伤害 2 → 3；酒力阈值 3 → 2；失去的天意之力 5 → 4
        DynamicVars.Damage.UpgradeValueBy(1);
        DynamicVars["wine_threshold"].UpgradeValueBy(-1);
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
    }
}
