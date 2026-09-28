using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using newsanguo.Scripts.Monsters;
using newsanguo.Scripts.Relics;

namespace newsanguo.Scripts.Patches;

// 事件「实践主义者」的战后奖励改写：把这场战斗的标准精英遗物奖励换成事件遗物「巨石」。
//
// 为什么必须用 Harmony：奖励组由 RewardsSet.WithRewardsFromRoom 组装（RewardsSet.cs:85-101），
// 而敌人遭遇与事件都不是钩子监听者——Hook.ModifyRewards 只遍历运行期订阅者
// （卡牌 / 遗物 / 药水 / Modifier / Badge 等，见 RewardsSet.cs:136），事件没法用钩子改奖励。
//
// 与 Pragmatist.Fight() 里 extraRewards 的关系：
//   · Pragmatist.Fight() 走「野生中立伏兵」同款路线，先追加一件「巨石」作为兜底；
//   · 本补丁的 Postfix 随后清掉奖励里所有 RelicReward（既包括随机精英遗物，也包括刚追加的那件巨石），
//     再放一件「巨石」——所以最终表现是「代替精英遗物」，且不会出现两件巨石；
//   · 万一补丁没挂上（补丁必须在 Scripts/Entry.cs 的列表里显式应用）或判定条件没命中，
//     extraRewards 那条路仍能保证「巨石」掉落。
//
// 判定条件（两者取或）：
//   1) 房间来自本事件——进入事件战斗时引擎会写下父事件 id（EventModel.cs:629 ParentEventId）；
//   2) 敌方就是盛碗虫（巨石）。
// 不能只用遭遇类型判定：CanonicalEncounter 在蜂巢以外的幕取的是本幕随机精英遭遇
// （Pragmatist.cs: runState.Act is Hive ? BowlbugBoulderEncounter : Act.PullNextEncounter(Elite)），
// 那些场次同样要换掉遗物奖励。
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
public static class PragmatistRewardPatch
{
    // 事件「实践主义者」的模型 id（内容注册日志：Registered act event: Pragmatist (id=NEWSANGUO_EVENT_PRAGMATIST)）
    private const string PragmatistEventEntry = "NEWSANGUO_EVENT_PRAGMATIST";

    [HarmonyPostfix]
    private static void ReplaceEliteRelic(RewardsSet __instance, AbstractRoom room)
    {
        if (__instance is null || !IsPragmatistAmbush(room))
        {
            return;
        }

        List<Reward> rewards = __instance.Rewards;
        // 随机精英遗物的奖励是延迟抽取的（RelicReward.Populate 才回调 RelicFactory），
        // 所以在这里移除不会白白消耗一次遗物抽取。
        rewards.RemoveAll(reward => reward is RelicReward);
        rewards.Add(new RelicReward(ModelDb.Relic<Boulder>().ToMutable(), __instance.Player));
    }

    // 这场战斗是不是「实践主义者」的伏击战
    private static bool IsPragmatistAmbush(AbstractRoom? room)
    {
        if (room is not CombatRoom combatRoom)
        {
            return false;
        }

        // 事件嵌套战斗：父事件 id 由 EnterCombatWithoutExitingEvent 写入
        if (combatRoom.ParentEventId is { } parentEventId &&
            string.Equals(parentEventId.Entry, PragmatistEventEntry, StringComparison.Ordinal))
        {
            return true;
        }

        // 兜底：蜂巢的巨石伏击
        return combatRoom.Encounter is BowlbugBoulderEncounter;
    }
}