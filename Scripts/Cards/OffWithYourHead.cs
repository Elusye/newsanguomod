using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 卡池归属由注册特性决定；保留原类名与存档 ID。
[RegisterCard(typeof(RetiredCardPool))]
public class OffWithYourHead : NewsanguoCardTemplate
{
    public override bool CanBeGeneratedByModifiers => Pool is not RetiredCardPool;
    public override bool CanBeGeneratedInCombat => Pool is not RetiredCardPool;


    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move),
        new RepeatVar(2)
    ];

    // 存在非攻击意图的敌人时金色高亮，提示多段攻击分支。
    protected override bool ShouldGlowGoldInternal =>
        base.CombatState?.HittableEnemies.Any(e => e.Monster?.IntendsToAttack == false) ?? false;

    public OffWithYourHead() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 非攻击意图：攻击 2/3 次；攻击意图：攻击 1 次。
        bool intendsToAttack = cardPlay.Target.Monster?.IntendsToAttack == true;
        int hitCount = !intendsToAttack ? DynamicVars["Repeat"].IntValue : 1;

        // 播放出牌音效：一次攻击与两次攻击使用不同的事件
        // 对应 FMOD 事件 event:/newsanguo/sfx/off_with_your_head / _double
        if (intendsToAttack)
        {
            NewsanguoSfx.Play("event:/newsanguo/sfx/off_with_your_head");
        }
        else
        {
            NewsanguoSfx.Play("event:/newsanguo/sfx/off_with_your_head_double");
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 升级提高非攻击意图分支的攻击次数，单段伤害不变。
        DynamicVars["Repeat"].UpgradeValueBy(1);
    }
}
