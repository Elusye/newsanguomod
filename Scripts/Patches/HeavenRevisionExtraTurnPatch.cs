using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterTakingExtraTurn))]
public static class HeavenRevisionExtraTurnPatch
{
    public static void Postfix(Player player, ref Task __result) =>
        __result = ReturnCards(__result, player);

    private static async Task ReturnCards(Task original, Player player)
    {
        await original;
        if (player.PlayerCombatState is not { } state)
            return;

        foreach (var card in state.AllCards.OfType<HeavenRevision>().ToArray())
            await card.ReturnToHand();
    }
}
