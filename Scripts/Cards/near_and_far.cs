using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class near_and_far : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：能力
    private const CardType type = CardType.Power;
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

    // 卡牌基础数值：本回合获得的力量、以及交替回合每次获得的量（升级后 4）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<near_and_far_power>("near_and_far_power", 3)
    ];

    // 鼠标悬停时显示力量与敏捷提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public near_and_far() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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
        NewsanguoSfx.Play("event:/newsanguo/sfx/near_and_far");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        int amount = DynamicVars["near_and_far_power"].IntValue;

        // 本回合内获得临时力量
        await PowerCmd.Apply<near_and_far_strength_power>(
            choiceContext,
            owner.Creature,
            amount,
            owner.Creature,
            this,
            silent: false);

        // 获得“忽近忽远”能力：接下来的回合交替获得临时敏捷与临时力量
        await PowerCmd.Apply<near_and_far_power>(
            choiceContext,
            owner.Creature,
            amount,
            owner.Creature,
            this,
            silent: false);
    }

    // 升级：获得的力量/敏捷 3 → 4
    protected override void OnUpgrade()
    {
        DynamicVars["near_and_far_power"].UpgradeValueBy(1);
    }
}
