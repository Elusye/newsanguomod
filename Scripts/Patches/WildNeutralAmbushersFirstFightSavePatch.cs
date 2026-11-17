using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using newsanguo.Scripts.Events;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.SaveRun), typeof(AbstractRoom), typeof(bool))]
public static class WildNeutralAmbushersFirstFightSavePatch
{
    public static bool Prefix(AbstractRoom? preFinishedRoom, bool saveProgress, ref Task __result)
    {
        // 首战胜利后仍留在同一事件，沿用进入事件前的跑局存档。
        if (saveProgress || preFinishedRoom is not CombatRoom
            {
                Encounter: WildNeutralAmbushersFirstEncounter,
                ShouldResumeParentEventAfterCombat: true,
                IsPreFinished: true
            })
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}
