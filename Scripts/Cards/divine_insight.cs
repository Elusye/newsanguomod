using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
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
public class divine_insight : NewsanguoCardTemplate
{
    // 基础耗能：3
    private const int energyCost = 3;
    // 卡牌类型：能力
    private const CardType type = CardType.Power;
    // 卡牌稀有度：先古（只能通过先古之民遗物等特殊途径获得，不进入普通卡牌奖励）
    private const CardRarity rarity = CardRarity.Ancient;
    // 目标类型（Self 表示对自己/玩家）
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡牌自带“虚无”关键词（升级后移除）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 悬停提示：展示“天意之力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<heavens_force>()
    ];

    // 卡牌基础数值：每打出一张牌获得 1 点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<divine_insight_power>("divine_insight_power", 1)
    ];

    public divine_insight() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        SfxCmd.Play("event:/newsanguo/sfx/divine_insight");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 附加“参悟天意”能力：每打出一张牌，获得对应点数的天意之力（可叠加）
        // 记录来源卡，打出本卡自身时不触发
        divine_insight_power? power = await PowerCmd.Apply<divine_insight_power>(
            choiceContext,
            owner.Creature,
            DynamicVars["divine_insight_power"].IntValue,
            owner.Creature,
            this,
            silent: false);
        power?.MarkAppliedBy(this);
    }

    // 升级后的效果逻辑：去除“虚无”
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）：加回“虚无”
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Ethereal);
    }
}
