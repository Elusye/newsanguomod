using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
public class peek_into_heaven : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 带“预见”关键词（悬停显示预见机制说明）
    protected override bool IsScryCard => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：预见数量、抽牌数、失去的天意之力（变量用正值，打出时取负）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("ScryAmount", 7m),
        new CardsVar(3),
        new PowerVar<heavens_force>("heavens_force", 3)
    ];

    // 鼠标悬停时显示天意之力与天意侵蚀提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay_power>()
    ];

    public peek_into_heaven() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/peek_into_heaven");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 预见（查看抽牌堆顶部 N 张，可选择丢弃任意张）
        await ScryCmd.Scry(choiceContext, base.Owner, DynamicVars["ScryAmount"].IntValue);

        // 抽 3 张牌
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, base.Owner);

        // 失去天意之力
        int lostAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<heavens_force>(
            choiceContext,
            base.Owner.Creature,
            -lostAmount,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 抽牌数从 3 增加到 4
        DynamicVars["Cards"].UpgradeValueBy(1);
        // 失去的天意之力从 3 减少到 2
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
    }
}
