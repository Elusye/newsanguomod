using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Patches;

/// <summary>地域词条自带保留行为，不额外添加会显示在卡面上的保留关键词。</summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldRetainThisTurn), MethodType.Getter)]
public static class TerritoryRetainPatch
{
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (TerritoryCardTemplate.HasTerritoryKeyword(__instance)) __result = true;
    }
}
