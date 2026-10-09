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

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 2026-10-08：按要求从「曹魏」（新三国）卡池移除，改为只注册在蜀汉卡池
// （同时已从 NewsanguoCardPool.CardTypes 中去掉，避免仍然命中曹魏池）
[RegisterCard(typeof(ShuHanCardPool))]
public class HeavenAndEarth : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的飞行层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<FlightPower>(3m)
    ];

    // 悬停提示：展示“飞行”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<FlightPower>()
    ];

    public HeavenAndEarth() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/heaven_and_earth");

        // 本卡不属于天意体系：不失去天意之力（IsHeavensCard 保持默认 false，不计入「恨天剑法」的天意牌统计）

        // 获得 3 层飞行
        int flightAmount = DynamicVars["FlightPower"].IntValue;
        await PowerCmd.Apply<FlightPower>(choiceContext, base.Owner.Creature, flightAmount, base.Owner.Creature, this);
    }

    // 升级后的效果逻辑：费用 2 → 1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
