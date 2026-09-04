using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;

namespace newsanguo.Scripts.Patches;

// 引擎级 FMOD 事件（角色选人 / 死亡）改走 NewsanguoSfx（Godot 资源播放）。
//
// newsanguo.bank 与 GUIDs.txt 已删除，FMOD 里不再存在这些事件；此前用 RitsuLib
// VirtualFmodEventRegistry 把它们映射到音频资源，但该链路依赖“已导入资源→私有缓存物化”，
// 音频未走 Godot 导入时静默失败。这里改为在引擎请求事件的唯一入口
// NAudioManager.PlayOneShot 处直接截获，转交 NewsanguoSfx 播放同名资源，
// 与其余卡牌/能力音效走同一条（支持裸文件读取的）播放链路，不再依赖 FMOD / 导入管线。
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot),
    new Type[] { typeof(string), typeof(Dictionary<string, float>), typeof(float) })]
public static class EngineSfxRedirectPatch
{
    // 本 mod 的引擎级事件路径；同名音频文件在 res://newsanguo/audios/ 下
    private static readonly HashSet<string> RedirectPaths = new(StringComparer.Ordinal)
    {
        "event:/newsanguo/sfx/character_select",
        "event:/newsanguo/sfx/character_death"
    };

    public static bool Prefix(NAudioManager __instance, string path, Dictionary<string, float> parameters, float volume)
    {
        if (path is not null && RedirectPaths.Contains(path))
        {
            // 已由 NewsanguoSfx 播放，跳过原生 FMOD 流程
            NewsanguoSfx.Play(path, volume);
            return false;
        }
        return true;
    }
}
