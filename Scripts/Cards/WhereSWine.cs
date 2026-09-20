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
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WhereSWine : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每次获得酒力时抽 2 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WhereSWinePower>(2m)
    ];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    public WhereSWine() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/where_s_wine");

        // 附加“哪里饮酒？”能力：获得酒力时抽牌
        int drawCount = DynamicVars["WhereSWinePower"].IntValue;
        await PowerCmd.Apply<WhereSWinePower>(
            choiceContext,
            base.Owner.Creature,
            drawCount,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 耗能从 1 降低到 0
        EnergyCost.UpgradeBy(-1);
    }
}
