using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “灵魂锁链”施加给所有玩家的“连环”：受到的伤害减少 50%；
/// 当你受到未被格挡的伤害时，其他玩家失去等量生命。
/// </summary>
[RegisterPower]
public class ChainPower : ModPowerTemplate
{
    // 伤害减免百分比
    private const decimal DamageReductionPercent = 50m;

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：单一（重复打出只刷新，不叠层）
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    // 伤害修正与受击钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 自己受到的攻击伤害减少 50%（乘法修正，与“飞行”同一算法）
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return (100m - DamageReductionPercent) / 100m;
    }

    // 自己受到未被格挡的伤害时，其他存活的玩家失去等量生命
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只处理本能力宿主受到的伤害，且只处理“攻击伤害”：
        // 下面的传导按“失去生命”（Unblockable + Unpowered）结算，不会被 IsPoweredAttack 命中，
        // 因此既不会被连环再次减半，也不会在玩家之间来回触发形成死循环。
        if (target != Owner || result.UnblockedDamage <= 0 || !props.IsPoweredAttack())
        {
            return;
        }

        // 找出其他存活的玩家盟友
        var combatState = Owner.CombatState;
        if (combatState is null)
        {
            return;
        }

        List<Creature> allies = combatState.GetTeammatesOf(Owner)
            .Where(c => c is not null && c != Owner && c.IsAlive && c.IsPlayer)
            .ToList();
        if (allies.Count == 0)
        {
            return;
        }

        foreach (Creature ally in allies)
        {
            await CreatureCmd.Damage(choiceContext, ally, result.UnblockedDamage, ValueProp.Unblockable | ValueProp.Unpowered, dealer: Owner, cardSource: null, cardPlay: null);
        }
    }
}
