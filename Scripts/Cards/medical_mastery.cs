using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class medical_mastery : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 基础伤害
    private const int baseDamage = 15;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 可被无限升级：升级来源（锻造/升级卡）按 IsUpgradable 过滤，
    // MaxUpgradeLevel 足够大即可反复升级（卡名会自动显示「医术高明+N」）
    public override int MaxUpgradeLevel => int.MaxValue;

    // 鼠标悬停时显示“斩杀”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Fatal)
    ];

    // 卡牌基础数值：造成 15 点伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(baseDamage, ValueProp.Move)
    ];

    public medical_mastery() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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
        NewsanguoSfx.Play("event:/newsanguo/sfx/medical_mastery");

        // 播放角色攻击动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Attack", base.Owner.Character.CastAnimDelay);

        // 斩杀判定：目标不是爪牙（如死亡不会触发斩杀的敌人）时，若被本次攻击杀死则触发
        bool shouldTriggerFatal = cardPlay.Target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

        AttackCommand attackCommand = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (shouldTriggerFatal &&
            attackCommand.Results.SelectMany(results => results).Any(result => result.WasTargetKilled))
        {
            // 斩杀：升级牌库中的自身——普通敌人升 1 级，精英/BOSS 战升 2 级
            RoomType? roomType = base.Owner.RunState.CurrentRoom?.RoomType;
            int upgradeCount = roomType is RoomType.Elite or RoomType.Boss ? 2 : 1;
            medical_mastery? deckCopy = base.DeckVersion as medical_mastery;
            if (deckCopy is null || upgradeCount <= 0)
            {
                return;
            }

            // CardCmd.Upgrade 在战斗即将结束时（IsEnding，例如斩杀的是最后一只敌人）会提前返回，
            // 导致升级被吞。此时延迟到战斗胜利（CombatWon，此时 IsEnding 已为 false）后再升级。
            if (CombatManager.Instance.IsEnding)
            {
                CombatManager.Instance.CombatWon += OnCombatWon;

                void OnCombatWon(CombatRoom _)
                {
                    CombatManager.Instance.CombatWon -= OnCombatWon;
                    UpgradeDeckVersion(deckCopy, upgradeCount);
                }
            }
            else
            {
                UpgradeDeckVersion(deckCopy, upgradeCount);
            }
        }
    }

    // 对牌库中的自身执行 upgradeCount 次升级
    private static void UpgradeDeckVersion(medical_mastery deckCopy, int upgradeCount)
    {
        for (int i = 0; i < upgradeCount; i++)
        {
            CardCmd.Upgrade(deckCopy);
        }
    }

    // 升级效果：造成的伤害增加 n+2，n 为升级次数。
    // UpgradeInternal 会先自增 CurrentUpgradeLevel 再调用本方法，
    // 所以第 n 次升级时 CurrentUpgradeLevel == n：第 1 次 +3、第 2 次 +4、第 3 次 +5……
    // 存档读档时引擎按升级次数重放本方法，数值自动保持一致。
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(CurrentUpgradeLevel + 2);
    }
}
