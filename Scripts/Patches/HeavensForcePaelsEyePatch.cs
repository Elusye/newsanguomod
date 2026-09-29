using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;

using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

// 天意给的额外回合不该“吃掉”原版遗物「佩尔之眼」（PaelsEye）。
//
// 症状（实机复现）：天意之力触发额外回合时，佩尔之眼的图标会闪一下，然后整场战斗都不再触发。
//
// 原因：引擎的额外回合是「先收集所有想给回合的模型，再统一派发」（CombatManager.cs:1360-1384）：
//     foreach (player) if (Hook.ShouldTakeExtraTurn(_state, player)) _playersTakingExtraTurn.Add(player);
//     ...
//     foreach (Player item in list) await Hook.AfterTakingExtraTurn(_state, item);
// 注意 AfterTakingExtraTurn 是**按玩家**派发给全部钩子监听者的——谁申请的这次回合并不参与派发。
// 于是 PaelsEye.AfterTakingExtraTurn 里那三件事
//     Flash(); Status = RelicStatus.Normal; UsedThisCombat = true;
// 会为天意授予的这次额外回合照常执行：图标闪一下（就是玩家看到的“闪烁”），
// 遗物被标记为本场战斗已用，之后再也触发不了。
//
// 处理：当天意的正向转化正在授予这次额外回合时，跳过 PaelsEye 的原方法体（Prefix 返回 false）：
// 不闪、不消耗。判定见 HeavensForcePower.IsExtraTurnGrantedByHeavens（标记在“下一次回合开始”才清除，
// 因为引擎对 AfterTakingExtraTurn 的派发顺序不保证，早清会让本补丁读不到）。
//
// 另注：PaelsEye 的另一半效果（BeforeSideTurnEndEarly 里“本回合没出牌就把手牌全消耗掉”）
// 发生在玩家回合结束的**更早**阶段（Hook.BeforeTurnEnd 的 Early 一档，早于天意弹选择界面），
// 因此不受本补丁影响，保持原版行为。
[HarmonyPatch(typeof(PaelsEye), nameof(PaelsEye.AfterTakingExtraTurn))]
public static class HeavensForcePaelsEyePatch
{
    /// <summary>
    /// 返回 false = 跳过原方法（不闪烁、不标记本场已用）；其余情况照原版执行。
    /// ⚠ 原方法是 async Task，而调用方是 <c>await model.AfterTakingExtraTurn(player)</c>（Hook.cs:1224），
    /// 所以跳过时必须自己给 <c>__result</c> 赋值 —— 否则 Harmony 会让它返回 null，
    /// 在 await 处直接抛 NullReferenceException，把整个额外回合/回合流程打断。
    /// </summary>
    private static bool Prefix(Player player, ref Task __result)
    {
        if (!HeavensForcePower.IsExtraTurnGrantedByHeavens(player))
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}
