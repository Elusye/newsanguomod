using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Patches;

// 修复非 Spine（纯 PNG 视觉）的新三国角色受到伤害时没有受伤音效的问题。
//
// 原版 CreatureCmd.Damage 只在“receiver 是怪物且怪物有受击音效”时才播受击声
// （SfxCmd.Play(receiver.Monster.HurtSfx)），玩家角色走不到该分支；
// 而非 Spine 角色没有骨架动画事件，也不会由视觉层发声，因此玩家受伤全程无声。
//
// Creature.LoseHpInternal 是所有伤害实际扣除生命的统一同步入口
// （CreatureCmd.Damage 的各个重载最终都会走到这里），在这里补播受伤音效，
// 与受到的实际掉血一一对应：被完全格挡（掉血 0）时不会触发。
// 音效直接走 NewsanguoSfx（Godot 播放），对应文件 audios/player_hurt.wav。
//
// 注意：必须精确限定为新闻三国角色本体（NewsanguoCharacter），不能用
// IModCharacterAssetOverrides 判断——那会把其他同样走 RitsuLib 管线的
// mod 角色（乃至其 PNG 角色）误判成本角色，导致别人受伤也播新三国音效。
[HarmonyPatch(typeof(Creature), nameof(Creature.LoseHpInternal))]
public static class PlayerHurtSfxPatch
{
    public static void Postfix(Creature __instance, DamageResult __result)
    {
        // 没有实际掉血（完全格挡/伤害为 0）时不播
        if (__result.UnblockedDamage <= 0)
        {
            return;
        }

        Player? player = __instance.Player;
        if (player == null)
        {
            return;
        }

        // 只处理新三国角色本身（纯 PNG 视觉，不会重复发声）
        if (player.Character is not NewsanguoCharacter)
        {
            return;
        }

        NewsanguoSfx.Play("event:/newsanguo/sfx/player_hurt");
    }
}
