using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using newsanguo.Scripts.Monsters;
using newsanguo.Scripts.Relics;

namespace newsanguo.Scripts.Patches;

// 事件「实践主义者」的战后奖励改写：**只有当对手就是盛碗虫（巨石）本尊**时，
// 把这场战斗的标准精英遗物奖励换成事件遗物「巨石」。
//
// 为什么必须用 Harmony：奖励组由 RewardsSet.WithRewardsFromRoom 组装（RewardsSet.cs:85-101），
// 而敌人遭遇与事件都不是钩子监听者——Hook.ModifyRewards 只遍历运行期订阅者
// （卡牌 / 遗物 / 药水 / Modifier / Badge 等，见 RewardsSet.cs:136），事件没法用钩子改奖励。
//
// 与 Pragmatist.Fight() 里 extraRewards 的关系（两处判定必须一致）：
//   · Fight() 只在遭遇是 BowlbugBoulderEncounter 时才追加一件「巨石」作兜底；
//   · 本补丁的 Postfix 随后清掉奖励里所有 RelicReward（既包括随机精英遗物，也包括刚追加的那件巨石），
//     再放一件「巨石」——所以最终表现是「代替精英遗物」，且不会出现两件巨石；
//   · 万一补丁没挂上（补丁必须在 Scripts/Entry.cs 的列表里显式应用）或判定条件没命中，
//     extraRewards 那条路仍能保证「巨石」掉落。
//
// 判定条件（两者同时成立）：
//   1) 敌方就是盛碗虫（巨石）——遭遇类型是 BowlbugBoulderEncounter。该遭遇类是**事件专用**的：
//      没有任何注册特性、全仓库只有 Pragmatist 用到它（BowlbugBoulder.cs:261，RoomType=Elite），
//      因此「遭遇是它」等价于「这场打的是盛碗虫（巨石）本尊」；
//   2) 房间来自本事件——EnterCombatWithoutExitingEvent 会写下父事件 id（实机 EventCombatSynchronizer
//      的 EnterCombat() 里 ParentEventId = _canonicalEvent.Id）。
//      ⚠ 之前这里是「两者取或」，于是蜂巢以外各幕的伏击（对手是本幕随机精英：
//      Pragmatist.cs 的 runState.Act is Hive ? BowlbugBoulderEncounter : Act.PullNextEncounter(Elite)）
//      也会掉「巨石」——现按需求收紧为**只有击败盛碗虫（巨石）本尊才掉**。
//      （巢穴幕的伏击对手现在只有 25% 概率是它，见 Pragmatist.BowlbugChancePercent；
//        掷空时对手是本幕随机精英，这里的类型判定自然不成立 → 发正常精英奖励。）
// 第 2 条成立失败时的表现是「随机精英遗物 + 兜底巨石」两件都在（多给而非少给），
// 不会出现「打赢巨石却什么都没掉」。
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
public static class PragmatistRewardPatch
{
    // 事件「实践主义者」的模型 id（内容注册日志：Registered act event: Pragmatist (id=NEWSANGUO_EVENT_PRAGMATIST)）
    private const string PragmatistEventEntry = "NEWSANGUO_EVENT_PRAGMATIST";

    [HarmonyPostfix]
    private static void ReplaceEliteRelic(RewardsSet __instance, AbstractRoom room)
    {
        if (__instance is null || !IsBowlbugBoulderAmbush(room))
        {
            return;
        }

        List<Reward> rewards = __instance.Rewards;
        // 随机精英遗物的奖励是延迟抽取的（RelicReward.Populate 才回调 RelicFactory），
        // 所以在这里移除不会白白消耗一次遗物抽取。
        rewards.RemoveAll(reward => reward is RelicReward);
        rewards.Add(new RelicReward(ModelDb.Relic<Boulder>().ToMutable(), __instance.Player));
    }

    // 这场战斗是不是「实践主义者」事件里与盛碗虫（巨石）本尊的伏击战
    private static bool IsBowlbugBoulderAmbush(AbstractRoom? room)
    {
        if (room is not CombatRoom combatRoom || combatRoom.Encounter is not BowlbugBoulderEncounter)
        {
            return false;
        }

        // 事件嵌套战斗：父事件 id 由 EnterCombatWithoutExitingEvent 写入
        return combatRoom.ParentEventId is { } parentEventId &&
               string.Equals(parentEventId.Entry, PragmatistEventEntry, StringComparison.Ordinal);
    }
}