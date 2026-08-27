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

// 别名指向“帝王之征”能力类，避免与同名卡牌类冲突
using dragon_omen_power = newsanguo.Scripts.Powers.dragon_omen;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class dragon_omen : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 0;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：普通
    private const CardRarity rarity = CardRarity.Common;
    // 目标类型：任意敌人（单体）
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：给予的帝王之征层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<dragon_omen_power>("dragon_omen", 4)
    ];

    // 悬停提示：展示“帝王之征”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<dragon_omen_power>()
    ];

    public dragon_omen() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        Creature? target = cardPlay.Target;
        ICombatState? combatState = base.CombatState;
        if (owner is null || target is null || combatState is null)
        {
            return;
        }

        // 播放出牌音效
        SfxCmd.Play("event:/newsanguo/sfx/dragon_omen");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 给予目标敌人 9（12）层帝王之征（单体给予）
        int amount = DynamicVars["dragon_omen"].IntValue;
        await PowerCmd.Apply<dragon_omen_power>(
            choiceContext,
            target,
            amount,
            owner.Creature,
            this,
            silent: false);

        // 所有拥有帝王之征的敌人失去与层数相等的生命（群体触发，不可格挡、不受力量等伤害修饰）
        foreach (var enemy in combatState.HittableEnemies)
        {
            dragon_omen_power? omen = enemy.GetPower<dragon_omen_power>();
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
