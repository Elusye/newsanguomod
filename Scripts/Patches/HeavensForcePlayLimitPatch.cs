using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.ShouldPlay))]
public static class HeavensForcePlayLimitPatch
{
    public static void Postfix(CardModel card, ref AbstractModel? preventer, ref bool __result)
    {
        if (!__result || card is not NewsanguoCardTemplate { LosesHeavensForce: true, IsFreeHeavensForce: false })
            return;

        if (HeavensForce.Get(card.Owner) > -HeavensForce.Threshold) return;

        preventer = card;
        __result = false;
    }
}
