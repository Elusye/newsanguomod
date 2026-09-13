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
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HeavenAndEarth : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：失去的天意之力、获得的飞行层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HeavensForcePower>(3m),
        new PowerVar<FlightPower>(3m)
    ];

    // 悬停提示：展示“天意之力”、“天意侵蚀”、“飞行”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>(),
        HoverTipFactory.FromPower<FlightPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public HeavenAndEarth() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/heaven_and_earth");

        // 失去 3 点天意之力
        int heavensLoss = DynamicVars["HeavensForcePower"].IntValue;
        await PowerCmd.Apply<HeavensForcePower>(choiceContext, base.Owner.Creature, -heavensLoss, base.Owner.Creature, this);

        // 获得 3 层飞行
        int flightAmount = DynamicVars["FlightPower"].IntValue;
        await PowerCmd.Apply<FlightPower>(choiceContext, base.Owner.Creature, flightAmount, base.Owner.Creature, this);
    }

    // 升级后的效果逻辑：费用 2 → 1，失去的天意之力 3 → 2
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
    }
}
