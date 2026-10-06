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
        // 通用选人音效（分角色音效缺失时的回退，见 Fallbacks）
        "event:/newsanguo/sfx/character_select",
        // 分角色选人音效：曹魏 / 蜀汉
        "event:/newsanguo/sfx/character_select_caowei",
        "event:/newsanguo/sfx/character_select_shuhan",
        "event:/newsanguo/sfx/character_death"
    };

    // 分角色选人音效尚未放进 res://newsanguo/audios/ 时（character_select_caowei.mp3 /
    // character_select_shuhan.mp3 还不存在），退回通用选人音效，避免选人时完全没声音。
    private static readonly Dictionary<string, string> Fallbacks = new(StringComparer.Ordinal)
    {
        ["event:/newsanguo/sfx/character_select_caowei"] = "event:/newsanguo/sfx/character_select",
        ["event:/newsanguo/sfx/character_select_shuhan"] = "event:/newsanguo/sfx/character_select"
    };

    public static bool Prefix(NAudioManager __instance, string path, Dictionary<string, float> parameters, float volume)
    {
        if (path is not null && RedirectPaths.Contains(path))
        {
            // 已由 NewsanguoSfx 播放，跳过原生 FMOD 流程。
            // Play 返回 null = mod 音效总开关关闭或音频资源缺失，此时才尝试回退（开关关闭时回退同样无声）。
            if (NewsanguoSfx.Play(path, volume) is null
                && Fallbacks.TryGetValue(path, out string? fallback))
            {
                NewsanguoSfx.Play(fallback, volume);
            }
            return false;
        }
        return true;
    }
}
