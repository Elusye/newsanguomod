using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Patches;

/// <summary>暂停钩子而非删除能力；同一次派发快照禁用对象，避免减层途中提前恢复。</summary>
[HarmonyPatch(typeof(CombatState), nameof(CombatState.IterateHookListeners))]
public static class AbilitySuppressionPatch
{
    public static void Postfix(CombatState __instance, ref IEnumerable<AbstractModel> __result)
    {
        var hanzhongOwners = __instance.Players.Where(player => AbilitySuppression.HoldsHanzhong(player.Creature))
            .Select(player => player.Creature).ToHashSet();
        var ironOwners = __instance.Enemies.Where(enemy => enemy.GetPowerAmount<IronCavalryPower>() > 0).ToHashSet();
        __result = Filter(__result, hanzhongOwners, ironOwners);
    }
    private static IEnumerable<AbstractModel> Filter(IEnumerable<AbstractModel> models,
        HashSet<Creature> hanzhongOwners, HashSet<Creature> ironOwners)
    {
        foreach (var model in models)
        {
            if (model is PowerModel power &&
                ((hanzhongOwners.Contains(power.Owner) && AbilitySuppression.CanSuppressWithHanzhong(power)) ||
                (ironOwners.Contains(power.Owner) && IronCavalryPower.CanSuppressPositivePower(power))))
                continue;
            yield return model;
        }
    }
}
