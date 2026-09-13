using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 注册卡牌到无色卡池（原版狂宴 Feed 的变体，额外添加“虚无”）
[RegisterCard(typeof(ColorlessCardPool))]
public class ChowDown : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：伤害与回复的生命值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new HealVar(2m)
    ];

    public ChowDown() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/chow_down");

        // 造成伤害并回复生命
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bite", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await CreatureCmd.Heal(base.Owner.Creature, DynamicVars.Heal.BaseValue);
    }

    // 升级：伤害 7 → 8，回复 2 → 3
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Heal.UpgradeValueBy(1m);
    }
}
