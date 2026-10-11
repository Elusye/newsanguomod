using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

/// <summary>过滤战斗钩子监听者，不移除能力、不调用 AfterRemoved；铁骑结束后自然恢复。</summary>
[HarmonyPatch(typeof(CombatState), nameof(CombatState.IterateHookListeners))]
public static class IronCavalryBuffSuppressionPatch
{
    public static void Postfix(CombatState __instance, ref IEnumerable<AbstractModel> __result)
    {
        // 固定本次派发的禁用名单：铁骑在敌方回合结束时消失，也不能让后面的能力提前恢复。
        HashSet<Creature> suppressed = __instance.Enemies
            .Where(enemy => enemy.GetPowerAmount<IronCavalryPower>() > 0).ToHashSet();
        __result = Filter(__result, suppressed);
    }

    private static IEnumerable<AbstractModel> Filter(IEnumerable<AbstractModel> listeners, HashSet<Creature> suppressed)
    {
        foreach (AbstractModel model in listeners)
        {
            if (model is PowerModel power && power.TypeForCurrentAmount == PowerType.Buff &&
                IsSupportedSource(power) &&
                power is not SandpitPower && power is not MinionPower &&
                power is not PossessStrengthPower && power is not PossessSpeedPower &&
                suppressed.Contains(power.Owner))
            {
                continue;
            }
            yield return model;
        }
    }

    private static bool IsSupportedSource(PowerModel power)
    {
        // 按实际能力类型的程序集判断，避免其他 mod 继承原版能力时也被禁用。
        var assembly = power.GetType().Assembly;
        return assembly == typeof(PowerModel).Assembly || assembly == typeof(IronCavalryPower).Assembly;
    }
}
