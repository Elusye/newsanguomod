using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
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
public class great_evil : NewsanguoCardTemplate
{
    // 基础耗能：2
    private const int energyCost = 2;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：普通
    private const CardRarity rarity = CardRarity.Common;
    // 目标类型：所有敌人
    private const TargetType targetType = TargetType.AllEnemies;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：对所有敌人造成 14 点伤害；获得 3 点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(14, ValueProp.Move),
        new PowerVar<heavens_force>("heavens_force", 3)
    ];

    // 悬停提示：展示“天意之力”与”天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay_power>()
    ];

    public great_evil() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        ICombatState? combatState = base.CombatState;
        if (combatState is null)
        {
            return;
        }

        SfxCmd.Play("event:/newsanguo/sfx/great_evil");

        // 播放角色攻击动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Attack", owner.Character.CastAnimDelay);

        // 对所有敌人造成 14 点伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .Execute(choiceContext);

        // 获得 2 点天意之力
        await PowerCmd.Apply<heavens_force>(
            choiceContext,
            owner.Creature,
            DynamicVars["heavens_force"].IntValue,
            owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 14 提高到 18
        DynamicVars.Damage.UpgradeValueBy(4);
        // 天意之力从 3 提高到 4
        DynamicVars["heavens_force"].UpgradeValueBy(1);
    }
}
