using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 曹魏专属的“防御”（2026-10-05 从 DefendNewsanguo 拆分而来）。
// 拆分原因同 StrikeCaowei：卡框颜色跟随所属卡池，两个角色各用自己卡池里的那张。
[RegisterCard(typeof(NewsanguoCardPool))]
[RegisterCharacterStarterCard(typeof(CaoWeiCharacter), 4)]
public class DefendCaowei : NewsanguoCardTemplate
{

    // 鼠标悬停时显示格挡提示
    public override bool GainsBlock => true;

    // 鼠标悬停时展示格挡说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    // 卡图资源：暂无专属立绘，沿用原 DefendNewsanguo 的卡图
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/DefendNewsanguo.png"
    );

    // 卡牌基础数值：格挡值
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(5, ValueProp.Move)
    ];

    // 标签：视为“防御”，使依赖防御标签的遗物/卡牌（如 leafy_poultice）可与之交互
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    public DefendCaowei() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/defend_newsanguo");

        // 获得格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, fast: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 格挡值从 5 提高到 8 (5+3)
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
