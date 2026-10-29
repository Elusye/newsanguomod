using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;
namespace newsanguo.Scripts.Patches;

/// <summary>仅修正曹魏颜色选项的显示文本，保留原选项和奖励回调。</summary>
[HarmonyPatch(typeof(ColorfulPhilosophers), "GenerateInitialOptions")]
public static class ColorfulPhilosophersCaoWeiTitlePatch
{
    private static readonly MethodInfo SetTitle = AccessTools.PropertySetter(typeof(EventOption), nameof(EventOption.Title));
    private static readonly MethodInfo SetDescription = AccessTools.PropertySetter(typeof(EventOption), nameof(EventOption.Description));
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(IReadOnlyList<EventOption> __result)
    {
        foreach (var option in __result)
        {
            if (option.TextKey != "COLORFUL_PHILOSOPHERS.pages.INITIAL.options.NEWSANGUO_CAOWEI") continue;
            // 使用独立键，防止颜色选项的通用回退文本代替本模组的颜色名。
            var title = new LocString("events", "NEWSANGUO_COLORFUL_PHILOSOPHERS_CAOWEI.title");
            SetTitle.Invoke(option, new object[] { title });
            var description = new LocString("events", "NEWSANGUO_COLORFUL_PHILOSOPHERS_CAOWEI.description");
            SetDescription.Invoke(option, new object[] { description });
            option.WithOverridenHistoryName(title);
        }
    }
}
