using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters;

namespace newsanguo.Scripts.Patches;

// 修复非 Spine（纯 PNG 视觉）mod 角色死亡时没有音效的问题。
//
// 原版 NCreature.StartDeathAnim 把“播放死亡音效 + 触发 Dead 动画”整体放在
// if (_spineAnimator != null) 块内；本 mod 角色视觉是纯 PNG（无 Spine 骨架），
// _spineAnimator 为 null，因此 SfxCmd.PlayDeath(Entity.Player) 永远不会被调用，
// 死亡音效完全没有触发点。
//
// RitsuLib 已通过 NCreatureNonSpineDeathAnimationTriggerPatch 在 StartDeathAnim
// 后缀补发 Dead 动画（所以能看到倒地动画），但它只补动画不补音效。
// 本补丁同样在 StartDeathAnim 后缀补播死亡音效，与 RitsuLib 的动画补发同一时机。
[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class PlayerDeathSfxPatch
{
    public static void Postfix(NCreature __instance)
    {
        Player? player = __instance.Entity?.Player;
        if (player == null)
        {
            return;
        }
        // 只处理走 RitsuLib 资源管线的 mod 角色（其 DeathSfx 已被替换为 mod 事件）
        if (player.Character is not IModCharacterAssetOverrides)
        {
            return;
        }
        // 有 Spine 动画时原版已经播过死亡音效，避免重复播放
        if (__instance.HasSpineAnimation)
        {
            return;
        }
        SfxCmd.PlayDeath(player);
    }
}
