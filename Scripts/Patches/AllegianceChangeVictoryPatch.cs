using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Patches;

// 等待全部胜利钩子结算后切换，早于 EndCombatInternal 的自动存档。
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatVictory))]
public static class AllegianceChangeVictoryPatch
{
    public static void Postfix(IRunState runState, ref Task __result) =>
        __result = Finish(__result, runState);

    private static async Task Finish(Task original, IRunState runState)
    {
        await original;
        await AllegianceChange.Complete(runState);
    }
}
