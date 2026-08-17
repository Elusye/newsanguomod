using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “飞行”：敌人对你造成的伤害降低50%。当你受到未被格挡的伤害时，飞行层数-1。
/// 层数归零时自动移除（替代原版“振翅”层数归零时的击晕逻辑，避免玩家被击晕导致卡死）。
/// </summary>
[RegisterPower]
public class flight_power : ModPowerTemplate
{
    // 敌人对你造成的伤害降低的百分比
    private const decimal DamageReductionPercent = 50m;

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，层数表示剩余可承受的未格挡伤害次数
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 受击钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 敌人对你造成的伤害降低50%（乘法修正）
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return (100m - DamageReductionPercent) / 100m;
    }

    // 当你受到未被格挡的伤害时，层数-1；层数归零时移除本能力
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage == 0 || !props.IsPoweredAttack())
        {
            return;
        }

        await PowerCmd.Decrement(this);
        if (Amount <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
