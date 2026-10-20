using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Combat;

/// <summary>汉中仅在持有者的手牌中生效；保留能力本体和层数，离手后恢复。</summary>
public static class AbilitySuppression
{
    internal static bool HoldsHanzhong(Creature owner) => owner.Player?.PlayerCombatState?.Hand.Cards
        .Any(card => card is AntiMagicField) == true;
    internal static bool IsModPower(PowerModel power) => power.GetType().Assembly == typeof(AntiMagicField).Assembly;
    internal static bool CanSuppressWithHanzhong(PowerModel power) =>
        IsModPower(power) && power is not FatherIHaveFailedYouPower and not RuffianPower;
    internal static bool IsSuppressed(PowerModel power) => power.IsMutable && CanSuppressWithHanzhong(power) && HoldsHanzhong(power.Owner);
}
