using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;


namespace newsanguo.Scripts.Cards;

// 注册卡牌到衍生卡池
// 「四轮车」：0 费衍生的技能牌，获得 4 点格挡 4 次（由「直奔诸葛亮四轮车！」生成）。
// 升级：每次获得的格挡 4 → 5（次数仍是 4 次）。
[RegisterCard(typeof(TokenCardPool))]
public class FourWheeledCart : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 词条：消耗（2026-10-09 追加；升级前、升级后都有，升级只加格挡）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 鼠标悬停时显示格挡提示
    public override bool GainsBlock => true;

    // 鼠标悬停时展示格挡说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    // 卡牌基础数值：格挡 4 点，结算 4 次
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(4m, ValueProp.Move),
        new RepeatVar(4)
    ];

    public FourWheeledCart() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 打出时的效果逻辑：按 Repeat 次数逐次获得格挡（每次都是独立的一次“获得格挡”，会分别触发相关效果）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/four_wheeled_cart");

        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, fast: false);
        }
    }

    // 升级：每次获得的格挡 4 → 5（次数仍是 4 次）。
    // 卡面文案不用改：描述里的 {Block:diff()} 会自动显示升级后的数值并高亮差值。
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}
