using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class FateUnknown : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 处决阈值：30（升级后 40）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("execute_threshold", 30)
    ];

    public FateUnknown() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        NewsanguoSfx.Play("event:/newsanguo/sfx/fate_unknown");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 若目标敌人的生命值小于等于阈值，则将其生命值变为 0（造成等同于其当前生命的不可阻挡伤害）
        Creature target = cardPlay.Target;
        int threshold = DynamicVars["execute_threshold"].IntValue;
        if (target.IsAlive && target.CurrentHp <= threshold)
        {
            await CreatureCmd.Damage(
                choiceContext,
                target,
                target.CurrentHp,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                this,
                cardPlay);
        }
    }

    // 升级后的效果逻辑：处决阈值 30 → 40
    protected override void OnUpgrade()
    {
        DynamicVars["execute_threshold"].UpgradeValueBy(10);
    }
}
