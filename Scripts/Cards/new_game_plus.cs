using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class new_game_plus : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：稀有
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 带“预见”关键词（悬停显示预见机制说明）
    protected override bool IsScryCard => true;

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：预见数量、天意之力、酒力、抽牌数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("ScryAmount", 3m),
        new PowerVar<heavens_force>("heavens_force", 2),
        new PowerVar<drunken_might>("drunken_might", 5),
        new CardsVar(1)
    ];

    // 鼠标悬停时显示天意之力、天意侵蚀与酒力提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay_power>(),
        HoverTipFactory.FromPower<drunken_might>()
    ];

    public new_game_plus() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/new_game_plus");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 预见（查看抽牌堆顶部 N 张，可选择丢弃任意张）
        await ScryCmd.Scry(choiceContext, owner, DynamicVars["ScryAmount"].IntValue);

        // 获得天意之力
        await PowerCmd.Apply<heavens_force>(
            choiceContext,
            owner.Creature,
            DynamicVars["heavens_force"].IntValue,
            owner.Creature,
            this,
            silent: false);

        // 获得酒力
        await PowerCmd.Apply<drunken_might>(
            choiceContext,
            owner.Creature,
            DynamicVars["drunken_might"].IntValue,
            owner.Creature,
            this,
            silent: false);

        // 抽 1 张牌
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, owner);
    }

    // 升级：获得“固有”
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    // 降级：移除“固有”
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Innate);
    }
}
