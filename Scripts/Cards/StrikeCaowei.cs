using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 曹魏专属的“打击”（2026-10-05 从 StrikeNewsanguo 拆分而来）。
// 拆分原因：卡框颜色由卡牌所属卡池决定（CardModel.Pool 取第一个包含该 id 的池），
// 原先两个角色共用 StrikeNewsanguo 且它只注册在新三国卡池里，蜀汉局里这张牌也会显示新三国的棕色边框。
// 现在曹魏用 StrikeCaowei、蜀汉用 StrikeShuhan，各自只属于本角色的卡池。
[RegisterCard(typeof(NewsanguoCardPool))]
[RegisterCharacterStarterCard(typeof(CaoWeiCharacter), 4)]
public class StrikeCaowei : NewsanguoCardTemplate
{

    // 卡图资源：暂无专属立绘，沿用原 StrikeNewsanguo 的卡图
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/StrikeNewsanguo.png"
    );

    // 卡牌基础数值
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6, ValueProp.Move)
    ];

    // 标签：视为“打击”，使依赖打击标签的遗物/卡牌（如 leafy_poultice）可与之交互
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    public StrikeCaowei() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效（资源文件 strike_newsanguo）
        NewsanguoSfx.Play("event:/newsanguo/sfx/strike_newsanguo");

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
