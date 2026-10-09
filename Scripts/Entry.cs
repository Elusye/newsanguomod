using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Patches;
using newsanguo.Scripts.Powers;
using newsanguo.Scripts.Relics;
using newsanguo.Scripts.Rewards;
using newsanguo.Scripts.Settings;
using newsanguo.Scripts.Telemetry;

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
        ApplyPatch(harmony, typeof(NewsanguoEnergyCounterPatch));
        ApplyPatch(harmony, typeof(AlwaysMineDiscardSelectPatch));
        ApplyPatch(harmony, typeof(AlwaysMineDiscardSelectionEndPatch));
        ApplyPatch(harmony, typeof(AlwaysMineDiscardGlowPatch));
        ApplyPatch(harmony, typeof(PlayerDeathSfxPatch));
        ApplyPatch(harmony, typeof(EngineSfxRedirectPatch));
        ApplyPatch(harmony, typeof(SecondAmountLabelPatch));
        ApplyPatch(harmony, typeof(HeavensForceHoverTipPatch));
        ApplyPatch(harmony, typeof(FatherIHaveFailedYouPowerPatch));
        ApplyPatch(harmony, typeof(RestSiteSmithSfxPatch));
        ApplyPatch(harmony, typeof(RestSiteHealSfxPatch));
        ApplyPatch(harmony, typeof(ReAddCardAfterPlayerChoicePatch));
        // 注意：补丁是**白名单**——新建的补丁类必须在这里显式加一行，否则只会被编译进 DLL 而永不生效
        // （此前 PragmatistRewardPatch 就是这样“看起来没生效”的）。
        ApplyPatch(harmony, typeof(PragmatistRewardPatch));
        // 天意给的额外回合不要再“吃掉”原版遗物「佩尔之眼」（PaelsEye）
        ApplyPatch(harmony, typeof(HeavensForcePaelsEyePatch));
        // 卡牌图鉴里的“规范牌”没有 Owner，只能靠“玩家当前浏览哪个卡池”来决定卡框颜色，
        // 否则蜀汉页的共享牌会一直用先命中的新三国池（棕色）——详见 CardLibraryPoolContext.cs
        ApplyPatch(harmony, typeof(CardLibraryPoolFilterPatch));
        ApplyPatch(harmony, typeof(CardLibraryPoolResetPatch));
        // 「创造模式」的选牌屏要像图鉴一样能搜索 / 筛选：复用图鉴的侧栏控件，只在那一屏生效
        // （详见 CardSelectSearchPatch.cs 顶部注释；标记由 CreativeModeReward 在弹屏前打开）
        ApplyPatch(harmony, typeof(CardSelectSearchPatch));
        // 「曹氏兵法」：①独立召唤的 1 生命值青州兵阵亡后立刻离场（否则尸体会留在 Pets 里）
        // ②敌人攻击的承伤优先级改成「青州兵（按召唤顺序依次穿透）→ 玩家格挡 → 奥斯提 → 玩家」
        //   （原版是「玩家格挡 → 奥斯提 → 玩家」，且多只召唤物时一次攻击只有 1 只接刀）
        ApplyPatch(harmony, typeof(CaoArtOfWarSoldierRemovalPatch));
        ApplyPatch(harmony, typeof(CaoArtOfWarSoldierDamageCascadePatch));
        ApplyPatch(harmony, typeof(SoldierDamageCascadeNestedHookSkipPatch));
        // ③青州兵的战斗表现：站位照原版奥斯提的锚点（主人右侧 + 抬高 75px）并在多只时横向错开，
        //   同时把引擎给「非奥斯提」宠物关掉的鼠标交互/血条恢复回来（否则鼠标悬浮看不到名字，见补丁顶部注释）
        ApplyPatch(harmony, typeof(CaoArtOfWarSoldierFormationPatch));
        ApplyPatch(harmony, typeof(CaoArtOfWarSoldierFormationRelayoutPatch));
        // “卡牌错位”归位清扫：出牌被取消 / 选牌结束时，把“逻辑上在手牌、画面却停在屏幕中央”的
        // 卡牌节点搬回手牌容器（详见 StrayHandCardCleanupPatch.cs 顶部注释）
        //
        // ⚠ 2026-10-01 曾停用（0.2.38 上线 → 联机不同步 → 回退 0.2.37 恢复正常）。
        // 【2026-10-03 结案，此前的归因是错的，留档以免重蹈】
        //   1) 先按版本对照怀疑到本补丁：0.2.38 相对 0.2.37 只多了它（Entry +6 / 补丁 +313），
        //      0.2.39 只多了没人调用的公开 API。但日志证据否掉了这个推论：
        //      .dsh-drop 里的 state divergence 发生在 0.2.26(09-20) 与 0.2.31(09-27)，
        //      而本补丁源码 09-30 才写出——时间上不可能。
        //   2) 本地双开测试也证明它不充分：启用本补丁仍不复现；
        //      另一次"启用后仍不复现"的结论同样是误导——因为当时没装孙乾 0.2.0 那个问题版本。
        //   3) 真正的元凶：孙乾宇宙（Sunqian Universe）0.2.0 改写无色卡池/无色药水内容时，
        //      两端算出的候选不一致 → 无色药水的玩家选择动作校验分歧 → 掉线。
        //      孙乾 0.2.1 更新后本地重测不再复现，结案。
        //   4) 本补丁与那次不同步无关；之所以继续停用，是因为玩家反馈"卡牌悬空"不影响实际游玩，
        //      不值得为纯视觉问题承担风险。若要恢复，请先按"不在选择流程里插延迟动作 + 全程
        //      try/catch + 联机下默认不启用"改写，并做联机实测。
        // ApplyPatch(harmony, typeof(StrayCardQueueCancelPatch));
        // ApplyPatch(harmony, typeof(StrayCardPlayCancelPatch));
        // ApplyPatch(harmony, typeof(StrayCardSelectionEndPatch));
        // ApplyPatch(harmony, typeof(StrayCardCombatEndPatch));
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        // 注册“天意之力”次级资源（必须在内容注册之前：卡牌动态变量与战斗 UI 都要用到它的完整 id）
        HeavensForce.Register();
        // 自动注册内容
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        // 注册自定义奖励类型（「创造模式」在战斗奖励屏上给的那张奖励）。
        // 必须在开局/读档之前完成：RitsuLib 靠这个注册表在存读档时按动态 RewardType 还原奖励。
        CreativeModeRewardRegistration.Register();
        // 先古之民遗物官方映射（由 RitsuLib 的补丁在事件/获得遗物时生效）：
        // 古老牙齿：把“仁之剑，义之剑”变化为先古卡“大奸似忠，大伪似真”
        RitsuLibFramework.RegisterArchaicToothTranscendenceMapping<BladesOfVirtue, TheTruestMask>(ModId);
        // 欧洛巴斯之触：把初始遗物“沛国佳酿”升级为先古遗物“百年佳酿”
        RitsuLibFramework.RegisterTouchOfOrobasRefinementMapping<FineBrewOfPei, CenturyBrew>(ModId);
        // 音频已全部迁移到 Godot 资源播放，不再注册 FMOD bank / GUIDs 映射（删除 newsanguo.bank 以减小体积）。
        // 卡牌/能力/事件音效经 NewsanguoSfx 直接播放音频文件；仅剩的两个引擎级 FMOD 事件
        // （角色选人 / 死亡）由 EngineSfxRedirectPatch 在 NAudioManager.PlayOneShot 入口
        // 截获并转交 NewsanguoSfx 播放同名音频资源，同样不再经过 FMOD。
        SubscribeAudioRestore();
        // RitsuLib Mod 设置页：注册本 mod 的设置项
        // （“音效音量”分区与控制台命令 newsanguo_sfx_volume 共用真值源；“测试模式”分区控制蜀汉角色是否可选）
        NewsanguoSfxVolumeSettings.Register();
        // 遥测（大盘层）：只申请 run_history（已结束跑局的原版 run-history，含每点的候选卡与是否被选）。
        // 后端地址在 NewsanguoTelemetry.IngestEndpoint；留空时该方法会直接返回、不做任何注册。
        NewsanguoTelemetry.Register();
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

    // 兜底恢复本机音量：若玩家在战斗中打出“扎聋我自己的耳朵！”后直接“保存并退出”，
    // 战斗结束钩子不会触发，音量减半会残留到下次启动。
    // 因此在保存完成、进入主菜单时统一恢复音量（重复调用无害）。
    // 注意：RunSavedEvent 在战斗结束时也会触发（游戏自动保存进度），因此不能在这里打断
    // “关羽之歌”——否则歌曲在战斗结束的瞬间就会被停掉。
    // 打断时机：SL 回到主菜单（MainMenuReadyEvent）或进入下一个房间（RoomEnteredEvent）。
    private static void SubscribeAudioRestore()
    {
        RitsuLibFramework.SubscribeLifecycle((IFrameworkLifecycleEvent evt) =>
        {
            if (evt is RunSavedEvent)
            {
                // 只恢复音量，不打断“关羽之歌”（战斗结束的自动保存也会走到这里）
                HearingVolumeController.RestoreFullVolume();
            }
            else if (evt is MainMenuReadyEvent)
            {
                // SL 保存并退出回到主菜单：恢复音量 + 打断“关羽之歌”
                HearingVolumeController.RestoreFullVolume();
                ReleasePower.StopSongOfGuanyu();
            }
            else if (evt is RoomEnteredEvent)
            {
                // 进入下一个房间（下一场战斗/事件/休息/商店等）时打断“关羽之歌”
                ReleasePower.StopSongOfGuanyu();
            }
        }, replayCurrentState: false);
    }
}