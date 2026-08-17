using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
/// “中原雄关”：下回合开始时按层数（Amount）获得格挡；
/// 若期间受到未被格挡的伤害，则失去所有层数（不再发放格挡）。
/// </summary>
[RegisterPower]
public class central_bastion_power : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，层数 = 下回合开始时获得的格挡
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合边界与受伤钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 玩家回合开始：按层数获得格挡，发放后移除能力
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player && Amount > 0)
        {
            // Amount 是打出时经 Hook.ModifyBlock 预计算的修正值（含敏捷加成），
            // 直接以 Unpowered 发放，避免下回合再次修正
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);

            // 触发中原雄关音效（对应 FMOD 事件 event:/newsanguo/sfx/central_bastion_power）
            SfxCmd.Play("event:/newsanguo/sfx/central_bastion_power");
            
            await PowerCmd.Remove(this);
        }
    }

    // 受到未被格挡的伤害：失去所有层数（下回合不再获得格挡）
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && dealer is not null && !dealer.IsPlayer && props.IsPoweredAttack() && result.UnblockedDamage > 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
