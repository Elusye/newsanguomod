using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

/// <summary>
/// 补丁共享状态（ThreadStatic：战斗只在主线程推进）。
/// IsDistributing：正在手工分派伤害，避免自己的递归调用再次进入补丁。
/// SkipNestedHooks：分派期间的嵌套 CreatureCmd.Damage 调用要跳过 Hook.ModifyDamage 与 Hook.BeforeDamageReceived，
/// 因为“伤害修正”与“即将受伤”已经在链头按玩家结算过一次
/// （否则易伤/虚弱会逐只青州兵重复计算，「荆棘」也会因为“即将受伤”跑两遍而打两下）。
/// </summary>
public static class SoldierDamageCascade
{
    [ThreadStatic]
    public static bool IsDistributing;

    [ThreadStatic]
    public static bool SkipNestedHooks;
}

/// <summary>
/// 「曹氏兵法」青州兵的承伤优先级（用户 2026-10-09 指定的顺序）：
///   ① 青州兵受伤（按召唤顺序依次穿透：最旧的一只先挨打，阵亡后溢出伤害转给下一只）
///   ② 扣除玩家格挡值
///   ③ 奥斯提受伤
///   ④ 玩家受伤
///
/// 原版 CreatureCmd.Damage 的顺序是「扣玩家格挡 → 改道给奥斯提（DieForYouPower.ModifyUnblockedDamageTarget，
/// 只会挑一只召唤物）→ 玩家」，且多只召唤物同时在场时一次攻击最多只有 1 只接刀、溢出直接落回玩家。
/// 本补丁把“单个目标就是玩家”的强化攻击整条伤害链接管过来：链头算一次伤害修正与“即将受伤”，
/// 然后 ① 手工按顺序打青州兵（带 Unblockable：格挡属于玩家，排在青州兵之后），
/// ②③④ 把余量伤害交回原版 CreatureCmd.Damage（目标仍是玩家、且不带 Unblockable），
/// 于是格挡、奥斯提改道、奥斯提溢出回到玩家、死亡结算全部复用引擎逻辑。
///
/// 手工重跑伤害管线的打法参照创意工坊「召唤独立」SummonOstys 的 OstyDamageDistributor（同一套 Hook 顺序 + 跳过嵌套钩子）。
/// 只在“「曹氏兵法」在场且还有它召唤的活青州兵”时接管，因此只影响“曹氏兵法”体系；
/// 一只青州兵都没有时完全走原版（奥斯提照旧按原版接刀）。
///
/// 注意：按上述优先级，青州兵一定先挨打（哪怕玩家格挡足够），这是需求里明确要求的顺序。
/// </summary>
[HarmonyPatch(typeof(CreatureCmd))]
public static class CaoArtOfWarSoldierDamageCascadePatch
{
    [HarmonyPatch(nameof(CreatureCmd.Damage), new[]
    {
        typeof(PlayerChoiceContext),
        typeof(IEnumerable<Creature>),
        typeof(decimal),
        typeof(ValueProp),
        typeof(Creature),
        typeof(CardModel),
        typeof(CardPlay)
    })]
    [HarmonyPrefix]
    public static bool Prefix(PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay, ref Task<IEnumerable<DamageResult>> __result)
    {
        if (SoldierDamageCascade.IsDistributing)
        {
            return true;
        }
        List<Creature> targetList = targets?.ToList() ?? new List<Creature>();
        // 只处理“单个目标就是玩家”的情形；其它目标（怪物、召唤物自己、多目标）交回原版
        if (targetList.Count != 1)
        {
            return true;
        }
        Creature playerCreature = targetList[0];
        if (!playerCreature.IsPlayer || playerCreature.Player is null)
        {
            return true;
        }
        // 与引擎一致：目标已死、或伤害来源已死（引擎会直接返回空结果），都交回原版
        if (playerCreature.IsDead || (dealer is not null && dealer.IsDead))
        {
            return true;
        }
        // 只有强化攻击才会被原版 DieForYouPower.ModifyUnblockedDamageTarget 改道给召唤物
        if (!props.IsPoweredAttack())
        {
            return true;
        }
        if (!CaoArtOfWarPower.HasLivingSummonedSoldier(playerCreature))
        {
            return true;
        }
        if (CaoArtOfWarPower.GetLivingSoldiersInSummonOrder(playerCreature).Count == 0)
        {
            return true;
        }
        SoldierDamageCascade.IsDistributing = true;
        __result = DistributeDamage(choiceContext, playerCreature, amount, props, dealer, cardSource, cardPlay);
        return false;
    }

    private static async Task<IEnumerable<DamageResult>> DistributeDamage(PlayerChoiceContext choiceContext, Creature playerCreature, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        try
        {
            List<DamageResult> allResults = new();
            var combatState = playerCreature.CombatState;
            IRunState runState = IRunState.GetFrom(new Creature[2] { playerCreature, dealer }.OfType<Creature>());

            // 链头：伤害修正 + “即将受伤”。整条链只按“打玩家”结算一次
            //（原版语义：ModifyDamage 之后、扣格挡之前触发 BeforeDamageReceived），
            // 所以易伤/虚弱/力量只算一遍，「荆棘」也只会反击一次。
            decimal modifiedAmount = Hook.ModifyDamage(runState, combatState, playerCreature, dealer, amount, props, cardSource, cardPlay, ModifyDamageHookType.All, CardPreviewMode.None, out IEnumerable<AbstractModel> mods);
            await Hook.AfterModifyingDamageAmount(runState, combatState, cardSource, mods);
            await Hook.BeforeDamageReceived(choiceContext, runState, combatState, playerCreature, modifiedAmount, props, dealer, cardSource);

            // ① 青州兵按召唤顺序依次穿透：最旧的一只先挨打，阵亡后溢出转给下一只；没死就由它吃掉全部余量。
            //    带 Unblockable：格挡属于玩家，按需求排在青州兵之后（②），不能在这里被扣掉。
            SoldierDamageCascade.SkipNestedHooks = true;
            decimal remainingDamage = modifiedAmount;
            // 伤害修正钩子跑完后再取一次名单（钩子里可能有青州兵已经死了）
            foreach (Creature soldier in CaoArtOfWarPower.GetLivingSoldiersInSummonOrder(playerCreature))
            {
                if (remainingDamage <= 0m)
                {
                    break;
                }
                if (!soldier.IsAlive)
                {
                    continue;
                }
                IEnumerable<DamageResult> soldierResults = await CreatureCmd.Damage(choiceContext, soldier, remainingDamage, props | ValueProp.Unblockable, dealer, cardSource, cardPlay);
                allResults.AddRange(soldierResults);
                if (soldier.IsAlive)
                {
                    // 这只没死就挡住了全部余量，后面的青州兵与玩家都不用再挨打
                    remainingDamage = 0m;
                    break;
                }
                DamageResult? lastSoldierResult = soldierResults.LastOrDefault();
                if (lastSoldierResult is null)
                {
                    // 理论上不会发生（引擎对已死目标会直接跳过）：余量交回原版结算
                    break;
                }
                remainingDamage = lastSoldierResult.OverkillDamage;
            }

            // ②③④ 余量交回原版：扣玩家格挡 → 奥斯提改道 → 玩家生命（奥斯提被打死时溢出部分也由原版还给玩家）。
            //        这一步不带 Unblockable，格挡才会在奥斯提之前被扣掉；SkipNestedHooks 仍然为 true。
            if (remainingDamage > 0m)
            {
                allResults.AddRange(await CreatureCmd.Damage(choiceContext, playerCreature, remainingDamage, props, dealer, cardSource, cardPlay));
            }
            return allResults;
        }
        finally
        {
            SoldierDamageCascade.SkipNestedHooks = false;
            SoldierDamageCascade.IsDistributing = false;
        }
    }
}

/// <summary>
/// 分派期间的嵌套 Damage 调用跳过两个钩子：
/// · Hook.ModifyDamage：伤害修正已在链头按玩家算过，逐只青州兵/余量再走一遍会把易伤、虚弱、力量重复计算；
/// · Hook.BeforeDamageReceived：“即将受伤”也已在链头按玩家触发过（原版「荆棘」就挂在这里），重复触发会打两下。
/// 只跳过这两个；格挡扣减、HpLost 两段相位、ModifyUnblockedDamageTarget（奥斯提改道）等全部照原版执行。
/// </summary>
[HarmonyPatch(typeof(Hook))]
public static class SoldierDamageCascadeNestedHookSkipPatch
{
    [HarmonyPatch(nameof(Hook.ModifyDamage))]
    [HarmonyPrefix]
    public static bool ModifyDamagePrefix(decimal damage, out IEnumerable<AbstractModel> modifiers, ref decimal __result)
    {
        if (SoldierDamageCascade.SkipNestedHooks)
        {
            modifiers = Enumerable.Empty<AbstractModel>();
            __result = damage;
            return false;
        }
        modifiers = null;
        return true;
    }

    [HarmonyPatch(nameof(Hook.BeforeDamageReceived))]
    [HarmonyPrefix]
    public static bool BeforeDamageReceivedPrefix(ref Task __result)
    {
        if (!SoldierDamageCascade.SkipNestedHooks)
        {
            return true;
        }
        __result = Task.CompletedTask;
        return false;
    }
}
