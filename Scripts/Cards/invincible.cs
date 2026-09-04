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
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class invincible : NewsanguoCardTemplate
{
    // 基础耗能：2
    private const int energyCost = 2;
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

    // 卡牌基础数值：对符合条件的敌人造成伤害增加 25%；打出时失去 3 点天意之力（升级后 2 点）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<heavens_force>("heavens_force", 3),
        new IntVar("bonus_percent", 25)
    ];

    public invincible() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 悬停提示：展示“飞行”、“振翅”、“翱翔”、“天意之力”与“天意侵蚀”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<flight_power>(),
        HoverTipFactory.FromPower<FlutterPower>(),
        HoverTipFactory.FromPower<SoarPower>(),
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay_power>()
    ];

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/invincible");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 打出时失去 3 点天意之力（升级后 2 点）
        await PowerCmd.Apply<heavens_force>(choiceContext, owner.Creature, -DynamicVars["heavens_force"].IntValue, owner.Creature, this);

        // 附加“天下无敌”能力：对没有振翅和翱翔的敌人造成伤害增加 25%
        // 效果可叠加：每次打出都会叠加对应百分比的增伤
        int bonusPercent = DynamicVars["bonus_percent"].IntValue;
        await PowerCmd.Apply<invincible_power>(choiceContext, owner.Creature, bonusPercent, owner.Creature, this);
    }

    // 升级后的效果逻辑：费用 2 → 1，失去的天意之力从 3 减少到 2
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
    }
}
