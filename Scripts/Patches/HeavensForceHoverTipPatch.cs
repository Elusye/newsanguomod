using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

// “天意之力”的悬停提示由 RitsuLib 依据资源定义生成，只含资源自身的说明，而其文案中会提到“天意侵蚀”。
// 这里在提示集合创建时为其补上“天意侵蚀”能力的说明，使两者成对展示。
// 资源提示既出现在战斗 HUD 的计数器上，也出现在卡面的天意之力费用条上，统一在这一处注入。
[HarmonyPatch(typeof(NHoverTipSet), "CreateAndShow",
    new[] { typeof(Control), typeof(IEnumerable<IHoverTip>), typeof(HoverTipAlignment) })]
public static class HeavensForceHoverTipPatch
{
    public static void Prefix(ref IEnumerable<IHoverTip> hoverTips)
    {
        // 只处理包含“天意之力”资源提示的集合（其余悬停提示原样放行）
        if (hoverTips is null || !hoverTips.Any(HeavensForce.IsOwnHoverTip))
        {
            return;
        }

        // 重复项由 NHoverTipSet 的 RemoveDupes 按 id 去重（卡牌自身已附带的“天意侵蚀”提示不会重复显示）
        hoverTips = hoverTips.Append(HoverTipFactory.FromPower<HeavensDecayPower>()).ToList();
    }
}
