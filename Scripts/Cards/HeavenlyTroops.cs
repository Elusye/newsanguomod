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

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HeavenlyTroops : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<Soldier>(IsUpgraded)
    ];

    // 卡牌基础数值：经过 2 个回合结束后发放 5 张士兵（TurnDelay 需与 heavenly_troops_power 的倒计时保持同步）；
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("SoldierCount", 5),
        new IntVar("TurnDelay", 2)
    ];

    public HeavenlyTroops() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/heavenly_troops");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 附加“天降雄兵”能力：经过 2 次玩家回合结束后，将对应数量的士兵加入手牌。
        // 升级后改为“天降雄兵+”，发放升级版“士兵+”。
        // 同一回合内打出多次会叠加士兵数量并重置倒计时；不同回合打出的各自独立倒计时。
        int soldierCount = DynamicVars["SoldierCount"].IntValue;
        int turnNumber = base.Owner.PlayerCombatState!.TurnNumber;
        if (IsUpgraded)
        {
            HeavenlyTroopsPlusPower? existingPlus = base.Owner.Creature
                .GetPowerInstances<HeavenlyTroopsPlusPower>()
                .FirstOrDefault(p => p.IsFromTurn(turnNumber));
            if (existingPlus != null)
            {
                await PowerCmd.ModifyAmount(choiceContext, existingPlus, soldierCount, base.Owner.Creature, this);
                existingPlus.ResetTurnsLeft();
            }
            else
            {
                HeavenlyTroopsPlusPower? plusPower = await PowerCmd.Apply<HeavenlyTroopsPlusPower>(
                    choiceContext,
                    base.Owner.Creature,
                    soldierCount,
                    base.Owner.Creature,
                    this);
                plusPower?.SetTurnNumber(turnNumber);
            }
        }
        else
        {
            HeavenlyTroopsPower? existing = base.Owner.Creature
                .GetPowerInstances<HeavenlyTroopsPower>()
                .FirstOrDefault(p => p.IsFromTurn(turnNumber));
            if (existing != null)
            {
                await PowerCmd.ModifyAmount(choiceContext, existing, soldierCount, base.Owner.Creature, this);
                existing.ResetTurnsLeft();
            }
            else
            {
                HeavenlyTroopsPower? power = await PowerCmd.Apply<HeavenlyTroopsPower>(
                    choiceContext,
                    base.Owner.Creature,
                    soldierCount,
                    base.Owner.Creature,
                    this);
                power?.SetTurnNumber(turnNumber);
            }
        }
    }
}
