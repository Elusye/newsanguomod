using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
public class desecrate_heaven : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
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

    // 卡牌基础数值：获得的天意之力、下个回合结束获得的天意侵蚀层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<heavens_force>("heavens_force", 15),
        new DynamicVar("DecayAmount", 15m)
    ];

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 鼠标悬停时显示天意之力与天意侵蚀提示；
    // 未升级时额外显示“保留”关键词说明（升级后获得保留词条会自动显示，避免重复）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            List<IHoverTip> tips =
            [
                HoverTipFactory.FromPower<heavens_force>(),
                HoverTipFactory.FromPower<heavens_decay>()
            ];

            if (!IsUpgraded)
            {
                tips.Add(HoverTipFactory.FromKeyword(CardKeyword.Retain));
            }

            return tips;
        }
    }

    public desecrate_heaven() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        SfxCmd.Play("event:/newsanguo/sfx/desecrate_heaven");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得天意之力
        int heavensForceAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<heavens_force>(
            choiceContext,
            base.Owner.Creature,
            heavensForceAmount,
            base.Owner.Creature,
            this,
            silent: false);

        // 在本回合保留手牌（原版“保留手牌”能力）
        await PowerCmd.Apply<RetainHandPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);

        // 下个回合结束时获得15层天意侵蚀（延迟标记能力，数值由变量传入）
        blasphemy_debt? debt = await PowerCmd.Apply<blasphemy_debt>(choiceContext, base.Owner.Creature, 2, base.Owner.Creature, this);
        debt?.SetDecayAmount(DynamicVars["DecayAmount"].IntValue);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 升级后获得“保留”关键词
        AddKeyword(CardKeyword.Retain);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Retain);
    }
}
