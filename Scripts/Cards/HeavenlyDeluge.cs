using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国（曹魏）专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HeavenlyDeluge : NewsanguoCardTemplate
{
    public override bool LosesHeavensForce => true;


    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 属于“天意”体系（涉及天意之力）
    public override bool IsHeavensCard => true;

    // 卡牌基础数值：对所有敌人造成 20（升级 26）点伤害；失去 5（升级 4）点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(20m, ValueProp.Move),
        new HeavensForceVar(5m)
    ];

    // 悬停提示：展示“天意之力”与“天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    public HeavenlyDeluge() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    // 升级后的效果逻辑：伤害 20 → 26；失去的天意之力 5 → 4
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/heavenly_deluge");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 对所有敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 无论当前天意之力多少，均击晕仍存活的敌人。
        foreach (Creature enemy in combatState.GetOpponentsOf(base.Owner.Creature).Where(c => c.IsAlive))
        {
            await CreatureCmd.Stun(enemy);
        }

        // 失去天意之力（本回合免费打出时由 HeavensForce.Add 内部免扣）
        await HeavensForce.Add(choiceContext, base.Owner, -DynamicVars["HeavensForcePower"].IntValue, this);
    }
}
