using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class ThreeBlades : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 11 点伤害 3 次；未击杀时失去 2 点生命
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(11, ValueProp.Move),
        new RepeatVar(3),
        new HpLossVar(2)
    ];

    public ThreeBlades() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        NewsanguoSfx.Play("event:/newsanguo/sfx/three_blades");

        // 造成 11 点伤害 3 次；若此牌未击杀敌人，你失去 2 点生命（斩杀判定参考原版 KnockoutBlow）
        bool killedEnemy = (await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext))
            .Results.SelectMany(results => results).Any(result => result.WasTargetKilled);
        if (!killedEnemy)
        {
            await CreatureCmd.Damage(
                choiceContext,
                base.Owner.Creature,
                DynamicVars.HpLoss.BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                this,
                cardPlay);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 11 提高到 14
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
