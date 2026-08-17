using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class heavenly_troops : NewsanguoCardTemplate
{
    // 基础耗能：3
    private const int energyCost = 3;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 悬停提示：展示“士兵”卡牌标注（升级时显示升级版士兵）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<soldier>(IsUpgraded)
    ];

    // 卡牌基础数值：经过 2 个回合结束后发放 5 张士兵（turn_delay 需与 heavenly_troops_power 的倒计时保持同步）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("soldier_count", 5),
        new IntVar("turn_delay", 2)
    ];

    public heavenly_troops() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        SfxCmd.Play("event:/newsanguo/sfx/heavenly_troops");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 附加“天降雄兵”能力：经过 2 次玩家回合结束后，将对应数量的士兵加入手牌。
        // 升级后改为“天降雄兵+”，发放升级版“士兵+”。
        // 同一回合内打出多次会叠加士兵数量并重置倒计时；不同回合打出的各自独立倒计时。
        int soldierCount = DynamicVars["soldier_count"].IntValue;
        int turnNumber = owner.PlayerCombatState?.TurnNumber ?? 0;
        if (IsUpgraded)
        {
            heavenly_troops_plus_power? existingPlus = owner.Creature
                .GetPowerInstances<heavenly_troops_plus_power>()
                .FirstOrDefault(p => p.IsFromTurn(turnNumber));
            if (existingPlus != null)
            {
                await PowerCmd.ModifyAmount(choiceContext, existingPlus, soldierCount, owner.Creature, this);
                existingPlus.ResetTurnsLeft();
            }
            else
            {
                heavenly_troops_plus_power? plusPower = await PowerCmd.Apply<heavenly_troops_plus_power>(
                    choiceContext,
                    owner.Creature,
                    soldierCount,
                    owner.Creature,
                    this);
                plusPower?.SetTurnNumber(turnNumber);
            }
        }
        else
        {
            heavenly_troops_power? existing = owner.Creature
                .GetPowerInstances<heavenly_troops_power>()
                .FirstOrDefault(p => p.IsFromTurn(turnNumber));
            if (existing != null)
            {
                await PowerCmd.ModifyAmount(choiceContext, existing, soldierCount, owner.Creature, this);
                existing.ResetTurnsLeft();
            }
            else
            {
                heavenly_troops_power? power = await PowerCmd.Apply<heavenly_troops_power>(
                    choiceContext,
                    owner.Creature,
                    soldierCount,
                    owner.Creature,
                    this);
                power?.SetTurnNumber(turnNumber);
            }
        }
    }
}
