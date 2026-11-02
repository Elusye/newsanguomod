using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using newsanguo.Scripts.Events;

namespace newsanguo.Scripts.Patches;

// SetEvent 同步创建嵌入战斗场景；在返回引擎绘制前隐藏尚未揭露的伏兵。
[HarmonyPatch(typeof(NCombatEventLayout), nameof(NCombatEventLayout.SetEvent))]
public static class PragmatistInitialVisibilityPatch
{
    public static void Postfix(NCombatEventLayout __instance, EventModel eventModel)
    {
        if (eventModel is Pragmatist pragmatist)
            pragmatist.InitializeEnemyVisibility(__instance.EmbeddedCombatRoom);
    }
}
