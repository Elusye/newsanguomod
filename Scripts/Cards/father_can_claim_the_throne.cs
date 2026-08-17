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
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class father_can_claim_the_throne : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：能力
    private const CardType type = CardType.Power;
    // 卡牌稀有度：稀有
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每回合开始时失去 3 点天意之力（升级后 2），获得 1 点能量并额外抽 1 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<father_can_claim_the_throne_power>("father_can_claim_the_throne_power", 3),
        new EnergyVar(1),
        new IntVar("draw_count", 1)
    ];

    // 鼠标悬停时显示天意之力与天意侵蚀提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay>()
    ];

    public father_can_claim_the_throne() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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
        SfxCmd.Play("event:/newsanguo/sfx/father_can_claim_the_throne");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 获得“称帝”能力：每回合开始时失去天意之力、获得能量并额外抽牌（三项均可叠加）
        int amount = DynamicVars["father_can_claim_the_throne_power"].IntValue;
        var power = await PowerCmd.Apply<father_can_claim_the_throne_power>(
            choiceContext,
            owner.Creature,
            amount,
            owner.Creature,
            this,
            silent: false);
        // 能量/抽牌数按 draw_count 叠加（已有实例则叠加，新实例从基础值开始）
        power?.AddCast(DynamicVars["draw_count"].IntValue);
    }

    // 升级：每回合失去的天意之力 3 → 2
    protected override void OnUpgrade()
    {
        DynamicVars["father_can_claim_the_throne_power"].UpgradeValueBy(-1);
    }
}
