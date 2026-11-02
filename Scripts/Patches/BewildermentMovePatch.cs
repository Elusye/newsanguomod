using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Combat.CardTargeting;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

/// <summary>只替换本次行动的效果，仍由原引擎记录行动、推进招式状态机和处理死亡清场。</summary>
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.PerformMove))]
public static class BewildermentMovePatch
{
    private static readonly HashSet<MonsterModel> Active = new();
    private static readonly FieldInfo OnPerform = AccessTools.Field(typeof(MoveState), "_onPerform")
        ?? throw new MissingFieldException(typeof(MoveState).FullName, "_onPerform");

    public static bool Prefix(MonsterModel __instance, ref Task __result)
    {
        if (Active.Contains(__instance) || __instance.Creature.Side != CombatSide.Enemy ||
            !__instance.Creature.IsAlive || __instance.Creature.GetPower<BewildermentPower>() is not { Amount: > 0 })
            return true;
        __result = PerformBewilderedMove(__instance);
        return false;
    }

    private static async Task PerformBewilderedMove(MonsterModel monster)
    {
        Active.Add(monster);
        MoveState? changedMove = null;
        object? original = null;
        try
        {
            Creature owner = monster.Creature;
            var targets = owner.CombatState!.HittableEnemies
                .Where(c => c != owner && c.IsAlive && c.Monster is not null).ToList();
            var attacks = monster.NextMove.Intents.OfType<AttackIntent>()
                .Select(a => (Damage: Math.Max(0m, a.DamageCalc?.Invoke() ?? 0m), Hits: Math.Max(1, a.Repeats)))
                .ToArray();
            owner.GetPower<BewildermentPower>()!.FlashForMove();
            if (attacks.Length == 0 || targets.Count == 0)
            {
                if (!owner.IsStunned)
                {
                    // 眩惑的击晕也覆盖必须先执行一次的招式，避免退回攻击玩家。
                    MoveState pending = monster.NextMove;
                    bool mustPerform = pending.MustPerformOnceBeforeTransitioning;
                    pending.MustPerformOnceBeforeTransitioning = false;
                    try { await CreatureCmd.Stun(owner); }
                    finally { pending.MustPerformOnceBeforeTransitioning = mustPerform; }
                }
            }
            else
            {
                // 采用跑局战斗目标 RNG，所有联机客户端会选择同一目标。
                Creature target = targets[monster.CombatState!.RunState.Rng.CombatTargets.NextInt(targets.Count)];
                changedMove = monster.NextMove;
                original = OnPerform.GetValue(changedMove);
                Func<IReadOnlyList<Creature>, Task> replacement = async _ =>
                {
                    foreach (var attack in attacks)
                    {
                        if (!owner.IsAlive || !target.IsAlive || CombatManager.Instance.IsOverOrEnding)
                            break;
                        await DamageCmd.Attack(attack.Damage).FromMonster(monster)
                            .TargetingFiltered([target]).WithHitCount(attack.Hits)
                            .WithHitFx("vfx/vfx_attack_slash").Execute(new ThrowingPlayerChoiceContext());
                    }
                };
                OnPerform.SetValue(changedMove, replacement);
            }
            // 内层调用由 Active 放行，保留引擎完整的行动生命周期。
            await monster.PerformMove();
        }
        finally
        {
            if (changedMove is not null && original is not null)
                OnPerform.SetValue(changedMove, original);
            Active.Remove(monster);
        }
    }
}
