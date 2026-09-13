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

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class OffWithYourHead : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(16m, ValueProp.Move)
    ];

    // 存在意图不是攻击的敌人时金色高亮（提示会攻击两次）
    protected override bool ShouldGlowGoldInternal =>
        base.CombatState?.HittableEnemies.Any(e => e.Monster?.IntendsToAttack != true) ?? false;

    public OffWithYourHead() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 如果敌人的意图不是攻击，则攻击两次（参考"他过江我也过江"的意图检测）
        bool intendsToAttack = cardPlay.Target.Monster?.IntendsToAttack == true;
        int hitCount = intendsToAttack ? 1 : 2;

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
        // 伤害从 16 提高到 20
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
