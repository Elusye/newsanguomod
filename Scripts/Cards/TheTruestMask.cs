using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class TheTruestMask : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 7 点伤害，攻击 2 次（升级后 11）；给予所有敌人 2 层虚弱与易伤
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7m, ValueProp.Move),
        new RepeatVar(2),
        new PowerVar<WeakPower>(2m),
        new PowerVar<VulnerablePower>(2m)
    ];

    // 悬停提示：展示“虚弱”与“易伤”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public TheTruestMask() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/the_truest_mask");

        // 对所有敌人造成 7 点伤害 2 次
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 给予所有敌人虚弱
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            combatState.HittableEnemies,
            DynamicVars.Weak.IntValue,
            base.Owner.Creature,
            this,
            silent: false);

        // 给予所有敌人易伤
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            combatState.HittableEnemies,
            DynamicVars.Vulnerable.IntValue,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 每次伤害从 7 提高到 11
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
