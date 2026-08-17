using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class brew_heals_all : NewsanguoCardTemplate
{
    // 基础耗能：2
    private const int energyCost = 2;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：5 点酒力、3 点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<drunken_might>("drunken_might", 5),
        new PowerVar<heavens_force>("heavens_force", 3)
    ];

    // 悬停提示：展示“酒力”、“天意之力”、“天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<drunken_might>(),
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay>()
    ];

    public brew_heals_all() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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
        SfxCmd.Play("event:/newsanguo/sfx/brew_heals_all");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 获得酒力与天意之力
        int drunkenMightAmount = DynamicVars["drunken_might"].IntValue;
        int heavensForceAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<drunken_might>(choiceContext, owner.Creature, drunkenMightAmount, owner.Creature, this, silent: false);
        await PowerCmd.Apply<heavens_force>(choiceContext, owner.Creature, heavensForceAmount, owner.Creature, this, silent: false);
    }

    // 升级：酒力 5 → 6，天意之力 3 → 4
    protected override void OnUpgrade()
    {
        DynamicVars["drunken_might"].UpgradeValueBy(1);
        DynamicVars["heavens_force"].UpgradeValueBy(1);
    }
}
