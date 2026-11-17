using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using newsanguo.Scripts.Events;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(BygoneEffigy), "GenerateMoveStateMachine")]
public static class WildNeutralAmbushersOpeningMovesPatch
{
    public static void Postfix(BygoneEffigy __instance, ref MonsterMoveStateMachine __result)
    {
        if (IsSecondFight(__instance))
            __result = new MonsterMoveStateMachine(__result.States.Values, __result.States["WAKE_MOVE"]);
    }

    private static readonly AccessTools.FieldRef<MonsterModel, Creature?> BoundCreature =
        AccessTools.FieldRefAccess<MonsterModel, Creature?>("_creature");

    // 预加载也会生成意图列表，此时可变怪物尚未绑定到 Creature。
    internal static bool IsSecondFight(MonsterModel monster) =>
        monster.IsMutable && BoundCreature(monster)?.CombatState?.Encounter is SkulkingColonyEffigyEncounter;
}

[HarmonyPatch(typeof(SlumberingBeetle), nameof(SlumberingBeetle.AfterAddedToRoom))]
public static class WildNeutralAmbushersBeetleSlumberPatch
{
    public static void Postfix(SlumberingBeetle __instance, ref Task __result)
    {
        if (WildNeutralAmbushersOpeningMovesPatch.IsSecondFight(__instance))
            __result = ReduceSlumber(__result, __instance);
    }

    private static async Task ReduceSlumber(Task original, SlumberingBeetle beetle)
    {
        await original;
        var creature = beetle.Creature;
        if (creature.GetPower<SlumberPower>() is { Amount: > 1 } slumber)
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), slumber, -1, creature, null, silent: true);
    }
}
