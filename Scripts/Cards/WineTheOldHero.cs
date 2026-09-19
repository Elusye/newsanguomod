using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Powers;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
// 说明：本牌原为“酒力减少时获得格挡”的能力牌，现改为“获得格挡 + 每点酒力额外获得格挡”的技能牌；
// 牌名（酒是老英雄）、卡图与出牌音效保持不变。
[RegisterCard(typeof(NewsanguoCardPool))]
public class WineTheOldHero : NewsanguoCardTemplate
{

    // 获得格挡：可被灵巧等格挡附魔识别
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：基础格挡 8；每有 1 点酒力额外获得 2 点格挡（升级 11 / 3）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(8m),
        new CalculationExtraVar(2m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
            (card, _) => card.Owner?.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0)
    ];

    // 鼠标悬停时显示格挡与酒力提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    public WineTheOldHero() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（沿用原牌音效）
        NewsanguoSfx.Play("event:/newsanguo/sfx/wine_the_old_hero");

        // 获得格挡（基础 8 + 酒力点数 × 每点额外格挡）
        await CreatureCmd.GainBlock(
            base.Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(null),
            DynamicVars.CalculatedBlock.Props,
            cardPlay);
    }

    // 升级：基础格挡 8 → 11，每点酒力的额外格挡 2 → 3
    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
        DynamicVars.CalculationExtra.UpgradeValueBy(1m);
    }
}
