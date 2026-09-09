using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class DragonOmen : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：给予的帝王之征层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DragonOmenPower>("dragon_omen", 4)
    ];

    // 悬停提示：展示“帝王之征”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DragonOmenPower>()
    ];

    public DragonOmen() : base(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/dragon_omen");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 给予目标敌人 4（6）层帝王之征（单体给予）
        await PowerCmd.Apply<DragonOmenPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["dragon_omen"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);

        // 所有拥有帝王之征的敌人失去与层数相等的生命（群体触发，不可格挡、不受力量等伤害修饰）
        foreach (var enemy in base.CombatState!.HittableEnemies)
        {
            DragonOmenPower? omen = enemy.GetPower<DragonOmenPower>();
            if (omen is not null && omen.Amount > 0)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    enemy,
                    omen.Amount,
                    ValueProp.Unblockable | ValueProp.Unpowered,
                    dealer: null,
                    cardSource: null,
                    cardPlay: cardPlay);
            }
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 帝王之征层数从 4 提高到 6
        DynamicVars["dragon_omen"].UpgradeValueBy(2);
    }
}
