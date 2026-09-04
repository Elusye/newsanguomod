using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Audio;

using newsanguo.Scripts.Patches;
using newsanguo.Scripts.Powers;
using newsanguo.Scripts.Relics;

namespace newsanguo.Scripts;

[ModInitializer(nameof(Init))]
public class Entry
{
    // 你的modid
    public const string ModId = "newsanguo";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        // 逐个应用 Harmony 补丁：单个补丁失败不影响其它补丁与 mod 主体，并在日志中记录失败原因
        var harmony = new Harmony("newsanguo");
        ApplyPatch(harmony, typeof(SetTextAutoSizeDiagnosticPatch));
        ApplyPatch(harmony, typeof(EstimateTextSizeDiagnosticPatch));
        ApplyPatch(harmony, typeof(AdjustFontSizeDiagnosticPatch));
        ApplyPatch(harmony, typeof(NewsanguoEnergyCounterPatch));
        ApplyPatch(harmony, typeof(CommanderArrivesSelectCardsPatch));
        ApplyPatch(harmony, typeof(CommanderArrivesSelectionEndPatch));
        ApplyPatch(harmony, typeof(CommanderArrivesGlowPatch));
        ApplyPatch(harmony, typeof(AlwaysMineDiscardSelectPatch));
        ApplyPatch(harmony, typeof(AlwaysMineDiscardSelectionEndPatch));
        ApplyPatch(harmony, typeof(AlwaysMineDiscardGlowPatch));
        ApplyPatch(harmony, typeof(PlayerDeathSfxPatch));
        ApplyPatch(harmony, typeof(PlayerHurtSfxPatch));
        ApplyPatch(harmony, typeof(EngineSfxRedirectPatch));
        ApplyPatch(harmony, typeof(SecondAmountLabelPatch));
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // 自动注册内容
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        // 先古之民遗物官方映射（由 RitsuLib 的补丁在事件/获得遗物时生效）：
        // 古老牙齿：把“仁之剑，义之剑”变化为先古卡“大奸似忠，大伪似真”
        RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<blade_of_virtue, the_truest_mask>(ModId);
        // 欧洛巴斯之触：把初始遗物“沛国佳酿”升级为先古遗物“百年佳酿”
        RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<fine_brew_of_pei, century_brew>(ModId);
        // 音频已全部迁移到 Godot 资源播放，不再注册 FMOD bank / GUIDs 映射（删除 newsanguo.bank 以减小体积）。
        // 卡牌/能力/事件音效经 NewsanguoSfx 直接播放音频文件；仅剩的两个引擎级 FMOD 事件
        // （角色选人 / 死亡）由 EngineSfxRedirectPatch 在 NAudioManager.PlayOneShot 入口
        // 截获并转交 NewsanguoSfx 播放同名音频资源，同样不再经过 FMOD。
        SubscribeAudioRestore();
    }

    private static void ApplyPatch(Harmony harmony, Type patchType)
    {
        try
        {
            harmony.CreateClassProcessor(patchType).Patch();
            Logger.Info($"Applied Harmony patch: {patchType.Name}");
            Diagnostics.Log($"Applied Harmony patch: {patchType.Name}");
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to apply Harmony patch {patchType.Name}: {e.Message}");
            Diagnostics.Log($"Failed to apply Harmony patch {patchType.Name}: {e}");
        }
    }

    // 兜底恢复全局静音：若玩家在战斗中打出“扎聋我自己的耳朵！”后直接“保存并退出”，
    // 战斗结束钩子不会触发，全局静音会残留到下次启动。
    // 因此在保存完成、进入主菜单时统一恢复声音（重复调用无害）。
    // 注意：RunSavedEvent 在战斗结束时也会触发（游戏自动保存进度），因此不能在这里打断
    // “关羽之歌”——否则歌曲在战斗结束的瞬间就会被停掉。
    // 打断时机：SL 回到主菜单（MainMenuReadyEvent）或进入下一个房间（RoomEnteredEvent）。
    private static void SubscribeAudioRestore()
    {
        RitsuLibFramework.SubscribeLifecycle((IFrameworkLifecycleEvent evt) =>
        {
            if (evt is RunSavedEvent)
            {
                // 只恢复全局静音，不打断“关羽之歌”（战斗结束的自动保存也会走到这里）
                FmodStudioMixerGlobals.TryUnmuteAllEvents();
                NewsanguoSfx.UnmuteAll();
            }
            else if (evt is MainMenuReadyEvent)
            {
                // SL 保存并退出回到主菜单：恢复声音 + 打断“关羽之歌”
                FmodStudioMixerGlobals.TryUnmuteAllEvents();
                NewsanguoSfx.UnmuteAll();
                release_power.StopSongOfGuanyu();
            }
            else if (evt is RoomEnteredEvent)
            {
                // 进入下一个房间（下一场战斗/事件/休息/商店等）时打断“关羽之歌”
                release_power.StopSongOfGuanyu();
            }
        }, replayCurrentState: false);
    }
}