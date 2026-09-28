using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class TenThousandTransparentHoles : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 1 点伤害 10 次
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(1m, ValueProp.Move)
    ];

    // 自带“消耗”关键词（合并 base 以保留模板附加的模组关键词）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    public TenThousandTransparentHoles() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/ten_thousand_transparent_holes");

        // 造成 1 点伤害 10 次；命中特效套用原版“穿刺”的金色刺击（NStabVfx）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(10)
            .WithHitVfxNode((Creature t) => NStabVfx.Create(t, facingEnemies: true, VfxColor.Gold))
            .Execute(choiceContext);
    }

    // 升级：费用 2 → 1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
