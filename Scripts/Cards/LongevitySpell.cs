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

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class LongevitySpell : NewsanguoCardTemplate
{

    // 不可通过战斗内的变化/随机生成获得（如“稍作修改”的变化）
    public override bool CanBeGeneratedInCombat => false;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡牌基础数值：失去 5 点天意之力（变量用正值，打出时取负）、获得 5 层再生
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HeavensForce>("heavens_force", 5),
        new PowerVar<RegenPower>("RegenPower", 5)
    ];

    // 鼠标悬停时显示再生、天意之力与天意侵蚀提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<RegenPower>(),
        HoverTipFactory.FromPower<HeavensForce>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    public LongevitySpell() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/longevity_spell");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 失去天意之力
        int lostAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<HeavensForce>(
            choiceContext,
            base.Owner.Creature,
            -lostAmount,
            base.Owner.Creature,
            this,
            silent: false);

        // 获得再生
        int regenAmount = DynamicVars["RegenPower"].IntValue;
        await PowerCmd.Apply<RegenPower>(
            choiceContext,
            base.Owner.Creature,
            regenAmount,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 失去的天意之力从 5 减少到 4，再生从 5 提高到 6
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
        DynamicVars["RegenPower"].UpgradeValueBy(1);
    }
}
