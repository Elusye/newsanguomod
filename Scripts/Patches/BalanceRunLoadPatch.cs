using System.Reflection;
using System.Text.Json.Nodes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using newsanguo.Scripts.Settings;
using STS2RitsuLib.RunData;

namespace newsanguo.Scripts.Patches;

// 牌组早于 RunState 返回就已反序列化。读档期间从同一份 RitsuLib 扩展数据读取规则。
[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable), typeof(SerializableRun))]
public static class BalanceRunLoadPatch
{
    private static readonly MethodInfo ReadPayload = typeof(RunSavedDataStore).Assembly
        .GetType("STS2RitsuLib.RunData.RunSavedDataRegistry")!
        .GetMethod("BuildPayloadFromSerializable", BindingFlags.Public | BindingFlags.Static)!;

    public static void Prefix(SerializableRun save, out bool? __state)
    {
        __state = BalanceAdjustments.RestoringRules;
        var payload = (string?)ReadPayload.Invoke(null, [save]);
        var data = payload is null ? null : JsonNode.Parse(payload);
        BalanceAdjustments.RestoringRules =
            data?["_ritsulib"]?["run_saved_data"]?[Entry.ModId]?[BalanceAdjustments.DataKey]?["data"]?["Enabled"]
                ?.GetValue<bool>() == true;
    }

    public static void Finalizer(bool? __state) => BalanceAdjustments.RestoringRules = __state;
}
