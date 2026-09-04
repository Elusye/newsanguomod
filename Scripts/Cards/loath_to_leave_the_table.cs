using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class loath_to_leave_the_table : NewsanguoCardTemplate
{
    // 基础耗能：3
    private const int energyCost = 3;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：稀有
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：所有敌人
    private const TargetType targetType = TargetType.AllEnemies;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：对所有敌人造成 25 点伤害；酒力阈值 10（升级后 8）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(25, ValueProp.Move),
        new IntVar("wine_threshold", 10)
    ];

    // 消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<drunken_might>()];

    // 酒力超过阈值时金色高亮（提示击晕与弃牌效果会触发）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            drunken_might? wine = base.Owner.Creature.GetPower<drunken_might>();
            return wine is not null && wine.Amount >= DynamicVars["wine_threshold"].IntValue;
        }
    }

    public loath_to_leave_the_table() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        var combatState = CombatState;
        if (combatState is null)
        {
            return;
        }

        // 打出此牌时快照酒力：判定必须在打出攻击牌后的酒力减半（AfterCardPlayed）之前，
        // 且不受伤害执行期间任何酒力变动的影响，因此先取快照再执行伤害。
        int wineAmount = owner.Creature.GetPower<drunken_might>()?.Amount ?? 0;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/loath_to_leave_the_table");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 对所有敌人造成 25 点伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .Execute(choiceContext);

        // 若你的酒力不小于阈值（基础 10，升级 8）：
        int wineThreshold = DynamicVars["wine_threshold"].IntValue;
        if (wineAmount >= wineThreshold)
        {
            // 击晕所有敌人
            foreach (Creature enemy in combatState.GetOpponentsOf(owner.Creature).Where(c => c.IsAlive))
            {
                await CreatureCmd.Stun(enemy);
            }

            // 所有玩家丢弃所有手牌
            foreach (Creature player in combatState.GetTeammatesOf(owner.Creature).Where(c => c.IsPlayer && c.IsAlive))
            {
                await CardCmd.Discard(choiceContext, PileType.Hand.GetPile(player.Player).Cards);
            }

            // 播放掀桌音效
            NewsanguoSfx.Play("event:/newsanguo/sfx/loath_to_leave_the_table_damage");
        }
    }

    // 升级后的效果逻辑：伤害 25 → 35；酒力阈值 10 → 8
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10);
        DynamicVars["wine_threshold"].UpgradeValueBy(-2);
    }
}
