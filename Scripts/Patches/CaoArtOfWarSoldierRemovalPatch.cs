using HarmonyLib;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

using newsanguo.Scripts.Powers;
using newsanguo.Scripts.Monsters;

namespace newsanguo.Scripts.Patches;

/// <summary>原版玩家死亡仅清理奥斯提，在能力被移除及停用钩子前补齐青州兵清理。</summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
public static class QingzhouSoldierOwnerDeathCleanupPatch
{
    public static void Postfix(Creature creature, bool wasRemovalPrevented, ref Task __result)
    {
        if (!wasRemovalPrevented && creature.Monster is QingzhouSoldier && creature.PetOwner is { } owner)
        {
            CaoArtOfWarPower.TryConsumeSummonedSoldier(owner.Creature, creature);
        }

        if (wasRemovalPrevented || !creature.IsPlayer)
        {
            return;
        }
        __result = CleanupAfterDeath(__result, creature);
    }

    private static async Task CleanupAfterDeath(Task originalDeathHooks, Creature creature)
    {
        await originalDeathHooks;
        // 死亡钩子可能复活主人；此时不动召唤物。
        if (!creature.IsDead || creature.Player?.PlayerCombatState is not { } combat)
        {
            return;
        }
        Creature[] soldiers = combat.Pets
            .Where(pet => pet.Monster is QingzhouSoldier && pet.IsAlive && pet.PetOwner == creature.Player)
            .ToArray();
        // 与主人死亡一致，强制清场，防止免死效果留下无主召唤物。
        await CreatureCmd.Kill(soldiers, force: true);
    }
}
