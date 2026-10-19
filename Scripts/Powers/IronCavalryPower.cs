using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using newsanguo.Scripts.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace newsanguo.Scripts.Powers;

/// <summary>界铁骑：暂时禁用指定范围的正面能力，敌方回合结束时减少一层。</summary>
[RegisterPower]
public class IronCavalryPower : ModPowerTemplate
{
    // 界铁骑额外保留潜地；时光酸雨仍按原清除范围处理。
    internal static bool CanSuppressPositivePower(PowerModel power) =>
        power is not BurrowedPower && TimeAcidRain.CanRemovePositivePower(power);

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png");
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy) await PowerCmd.TickDownDuration(this);
    }
}
