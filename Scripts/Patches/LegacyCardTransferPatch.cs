using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Patches;
[HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
public static class LegacyCardTransferPatch
{
    public static void Postfix(CardModel __instance, ref Task __result)
    {
        if (__instance is NewsanguoCardTemplate card) __result = Complete(__result, card);
    }
    private static async Task Complete(Task play, NewsanguoCardTemplate card)
    {
        await play;
        await card.CompleteLegacyTransfer();
    }
}