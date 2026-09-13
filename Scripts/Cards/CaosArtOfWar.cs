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

// 曹氏兵法：每当你打出一张牌时召唤 1；每当任何生物死亡时你获得 2 点力量。可叠加。
// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class CaosArtOfWar : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：打出一张牌时召唤 1（奥斯蒂生命值）；任何生物死亡时获得 1 点力量。
    // 两处数值都随打出的“曹氏兵法”数量叠加，由能力 caos_art_of_war_power 的层数体现。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new SummonVar(1),
        new PowerVar<StrengthPower>(1m)
    ];

    // 悬停提示：展示“召唤（奥斯蒂）”与“力量”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.SummonDynamic, base.DynamicVars.Summon),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public CaosArtOfWar() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/caos_art_of_war");

        // 获得“曹氏兵法”能力（可叠加：每次打出令层数 +1，触发强度随之提高）。
        // 无需记录来源卡：能力在本次出牌结算中才生效，本次出牌不在能力的账本里，自然不会触发
        await PowerCmd.Apply<CaosArtOfWarPower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级：费用 2 → 1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
