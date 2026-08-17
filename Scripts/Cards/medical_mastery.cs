using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
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

    // 斩杀成长的基础伤害（不随升级变化）
    private const int baseDamage = 15;

    // 注意：v0.111.0 起存档属性改由 ModelIdSerializationCache 自动扫描 ModelDb 中的类型，
    // 本卡注册进 ModelDb 后其 [SavedProperty] 会被自动收录，无需手动注入（旧 API 已删除）。

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 鼠标悬停时显示“斩杀”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Fatal)
    ];

    // 当前伤害（含斩杀成长），随存档持久化
    private int _currentDamage = baseDamage;

    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            // 同步卡面显示的基础伤害
            base.DynamicVars.Damage.BaseValue = _currentDamage;
        }
    }

    // 累计斩杀成长值，随存档持久化
    private int _increasedDamage;

    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
        }
    }

    // 卡牌基础数值：造成当前伤害（初始 15 点）；斩杀成长：普通 +3（升级 +5）、精英/BOSS +5（升级 +7）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(CurrentDamage, ValueProp.Move),
        new IntVar("fatal_bonus", 3),
        new IntVar("fatal_bonus_elite", 5)
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
        SfxCmd.Play("event:/newsanguo/sfx/medical_mastery");

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
            // 精英/BOSS 战获得更多永久提升（fatal_bonus_elite > fatal_bonus）
            RoomType? roomType = base.Owner.RunState.CurrentRoom?.RoomType;
            bool isEliteOrBoss = roomType is RoomType.Elite or RoomType.Boss;
            int permanentBonus = isEliteOrBoss
                ? DynamicVars["fatal_bonus_elite"].IntValue
                : DynamicVars["fatal_bonus"].IntValue;

            // 本局游戏永久成长：战斗中的卡牌与牌库中的卡牌同时提升（与“遗传算法”一致）
            BuffFromPlay(permanentBonus);
            (base.DeckVersion as medical_mastery)?.BuffFromPlay(permanentBonus);
        }
    }

    // 斩杀后累计成长值并刷新伤害
    private void BuffFromPlay(int bonus)
    {
        IncreasedDamage += bonus;
        UpdateDamage();
    }

    // 伤害 = 基础伤害 + 累计成长值
    private void UpdateDamage()
    {
        CurrentDamage = baseDamage + IncreasedDamage;
    }

    // 战斗中降级会重建 DynamicVars（恢复为卡池初始值），需把斩杀成长后的伤害重新同步回卡面
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        base.DynamicVars.Damage.BaseValue = _currentDamage;
    }

    // 升级后的效果逻辑：斩杀成长 +3/+5 → +5/+7
    protected override void OnUpgrade()
    {
        DynamicVars["fatal_bonus"].UpgradeValueBy(2);
        DynamicVars["fatal_bonus_elite"].UpgradeValueBy(2);
    }
}
