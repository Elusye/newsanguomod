using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using newsanguo.Scripts.Events;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(CombatManager), "StartCombatInternal")]
public static class WildNeutralAmbushersReplayPatch
{
    public static void Prefix()
    {
        var manager = RunManager.Instance;
        if (manager.DebugOnlyGetState()?.CurrentRoom is not CombatRoom { Encounter: SkulkingColonyEffigyEncounter })
            return;

        var writer = manager.CombatReplayWriter;
        if (writer.IsEnabled && !writer.IsRecordingReplay)
            writer.RecordInitialState(manager.ToSave(null));
    }
}
