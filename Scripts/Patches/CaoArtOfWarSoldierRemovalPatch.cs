using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;

using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

// 「曹氏兵法」独立召唤的青州兵是 1 生命值的消耗品：死掉就该离场，而不是像原版奥斯提那样留在场上等下次召唤复活
// （留在 Pets 里的尸体还会让 Player.Osty / IsOstyAlive 的“取第一只”指向尸体，影响「自刎归天」等改道效果）。
//
// 接入点：DieForYouPower.ShouldCreatureBeRemovedFromCombatAfterDeath。
// 引擎的裁定是“所有监听者都同意才移除”（Hook.ShouldCreatureBeRemovedFromCombatAfterDeath，
// .decompile/sts2full/sts2.decompiled.cs:166650-166660），而 DieForYouPower 对自己的主人恒返回 false
// （:100184-100191：主人就是召唤物自己），所以只能在这里改口。
//
// 只对本能力召唤的青州兵生效（CaoArtOfWarPower.TryConsumeSummonedSoldier 命中即认领并消费该个体），
// 原版死灵缚者的奥斯提、以及「古挽歌」走 OstyCmd.Summon 召唤的那只都不受影响。
[HarmonyPatch(typeof(DieForYouPower), nameof(DieForYouPower.ShouldCreatureBeRemovedFromCombatAfterDeath))]
public static class CaoArtOfWarSoldierRemovalPatch
{
    public static bool Prefix(DieForYouPower __instance, Creature creature, ref bool __result)
    {
        if (creature != __instance.Owner)
        {
            return true;
        }
        Player? owner = creature.PetOwner;
        if (owner?.Creature is null || !CaoArtOfWarPower.TryConsumeSummonedSoldier(owner.Creature, creature))
        {
            return true;
        }

        // 这只青州兵是「曹氏兵法」召唤的独立个体：同意移除（引擎随即把它移出 Pets 并删除节点）
        __result = true;
        return false;
    }
}
