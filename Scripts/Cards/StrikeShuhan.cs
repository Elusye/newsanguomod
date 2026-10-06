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
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 蜀汉专属的“打击”（2026-10-05 从 StrikeNewsanguo 拆分而来）。
// 只注册在蜀汉卡池里，牌框因此跟随蜀汉的墨绿配色（详见 StrikeCaowei 的注释）。
[RegisterCard(typeof(ShuHanCardPool))]
[RegisterCharacterStarterCard(typeof(ShuHanCharacter), 4)]
public class StrikeShuhan : NewsanguoCardTemplate
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

    public StrikeShuhan() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
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
