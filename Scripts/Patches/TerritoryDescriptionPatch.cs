using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Patches;

/// <summary>仅隐藏地域牌描述中自动生成的保留行，不改实际保留判定。</summary>
[HarmonyPatch]
public static class TerritoryDescriptionPatch
{
    public static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(CardModel))
        .Single(method => method.Name == "GetDescriptionForPile" && method.GetParameters().Length == 3);
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getter = AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.ShouldRetainThisTurn));
        var replacement = AccessTools.Method(typeof(TerritoryDescriptionPatch), nameof(ShouldShowRetain));
        int replaced = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(getter))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
                replaced++;
            }
            yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException($"Expected one Retain description check, found {replaced}.");
    }
    private static bool ShouldShowRetain(CardModel card) =>
        !TerritoryCardTemplate.HasTerritoryKeyword(card) && card.ShouldRetainThisTurn;
}
