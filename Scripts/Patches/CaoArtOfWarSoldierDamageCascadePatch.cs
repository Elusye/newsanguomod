using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using newsanguo.Scripts.Monsters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPreBlockDamageTarget))]
public static class QingzhouSoldierPreBlockTargetPatch
{
    public static void Postfix(Creature originalTarget, ValueProp props, ref Creature? __result)
    {
        if (originalTarget.IsAlive && props.IsPoweredAttack())
        {
            __result = CaoArtOfWarPower.GetLivingSoldiersInSummonOrder(originalTarget).FirstOrDefault() ?? __result;
        }
    }
}

[HarmonyPatch]
public static class CaoArtOfWarSoldierDamageCascadePatch
{
    private static MethodBase TargetMethod()
    {
        var damage = AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
            [typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal),
             typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType)]);
        return AccessTools.AsyncMoveNext(damage);
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var preBlock = AccessTools.Method(typeof(Hook), nameof(Hook.ModifyPreBlockDamageTarget));
        var overkill = AccessTools.PropertyGetter(typeof(DamageResult), nameof(DamageResult.OverkillDamage));
        var addResult = AccessTools.Method(typeof(List<DamageResult>), nameof(List<DamageResult>.Add));

        int preBlockIndex = code.FindIndex(instruction => instruction.Calls(preBlock));
        int index = preBlockIndex < 0 ? -1 : code.FindIndex(preBlockIndex + 1, instruction => instruction.Calls(overkill));
        // 原版把首个格挡前承伤结果加入列表，再读取该结果的溢出量。
        if (index < 6 || !code[index - 3].Calls(addResult) ||
            code[index - 5].operand is not FieldInfo field || field.FieldType != typeof(List<DamageResult>) ||
            code[index - 6].opcode != OpCodes.Ldarg_0)
        {
            throw new InvalidOperationException("Could not locate pre-block damage overflow in CreatureCmd.Damage.");
        }

        var readOverflow = code[index];
        var loadStateMachine = new CodeInstruction(OpCodes.Ldarg_0);
        loadStateMachine.labels.AddRange(readOverflow.labels);
        readOverflow.labels.Clear();
        code.InsertRange(index, [loadStateMachine, new CodeInstruction(OpCodes.Ldfld, field)]);
        readOverflow.opcode = OpCodes.Call;
        readOverflow.operand = AccessTools.Method(typeof(CaoArtOfWarSoldierDamageCascadePatch), nameof(DistributeOverflow));
        return code;
    }

    private static int DistributeOverflow(DamageResult first, List<DamageResult> results)
    {
        int remaining = first.OverkillDamage;
        if (remaining <= 0 || first.Receiver.Monster is not QingzhouSoldier ||
            first.Receiver.PetOwner?.Creature != first.OriginalTarget)
        {
            return remaining;
        }

        foreach (Creature soldier in CaoArtOfWarPower.GetLivingSoldiersInSummonOrder(first.OriginalTarget))
        {
            if (remaining <= 0)
            {
                break;
            }
            if (soldier == first.Receiver || !soldier.IsAlive)
            {
                continue;
            }

            // 与瓦皮相同，格挡前直接扣生命，结果交由原版派发伤害和死亡回调。
            DamageResult result = soldier.LoseHpInternal(remaining, first.Props);
            result.OriginalTarget = first.OriginalTarget;
            result.IntendedHpLoss = remaining;
            results.Add(result);
            remaining = result.OverkillDamage;
        }
        return remaining;
    }
}
