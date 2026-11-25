using System.Collections;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Cards;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Patches;

// 可选兼容：扩展对方的类型映射，不复制音频，也不改变其播放与音量逻辑。
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCardPlayed))]
public static class OldTarotVoiceCompatibilityPatch
{
    private static bool _registered;
    private static readonly (Type Original, Type Old)[] Mappings =
    [
        (typeof(MythicForm), typeof(OldMythicForm)),
        (typeof(TheHierophant), typeof(OldTheHierophant)),
        (typeof(TheEmperor), typeof(OldTheEmperor)),
        (typeof(TheTower), typeof(OldTheTower)),
        (typeof(Temperance), typeof(OldTemperance)),
        (typeof(TheSun), typeof(OldTheSun)),
        (typeof(Death), typeof(OldDeath))
    ];
    [HarmonyPriority(Priority.First)]
    public static void Prefix(CardPlay cardPlay)
    {
        if (_registered || !Mappings.Any(pair => pair.Old == cardPlay.Card.GetType())) return;
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
            a.GetType("FoolTarotVoice.TarotVoice", false) is not null);
        if (assembly is null) return;
        var audio = assembly.GetType("FoolTarotVoice.TarotVoice")?.GetField("AudioFiles",
            BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary;
        var captions = assembly.GetType("FoolTarotVoice.TarotBanner")?.GetField("Descriptions",
            BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary;
        if (audio is null || captions is null) return;
        foreach (var pair in Mappings)
        {
            if (audio.Contains(pair.Original)) audio[pair.Old] = audio[pair.Original];
            if (captions.Contains(pair.Original)) captions[pair.Old] = captions[pair.Original];
        }
        _registered = true;
    }
}
