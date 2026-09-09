using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace newsanguo.Scripts;

/// <summary>
/// 全 mod 音效统一播放入口。
///
/// 说明：音效已全面脱离 FMOD——卡牌/能力/事件直接经这里用 Godot AudioStreamPlayer 播放
/// res://newsanguo/audios/ 下的音频资源（支持 .mp3/.wav/.ogg，自动探测扩展名，与“关羽之歌”同款做法）。
/// 角色选人/死亡两个由引擎触发的 FMOD 事件，则经 EngineSfxRedirectPatch 截获后同样走这里播放同名资源。
///
/// 音量构成（按序叠加）：
/// 1) 本 mod 总开关 SfxEnabled（关闭后不播放任何音效，已响起的即时压静音）；
/// 2) 跟随游戏内“音效”音量滑杆（CurrentSfxOptionDb，与原生 FMOD 音效总线同一“选项²”曲线）；
/// 3) 本 mod 专属倍率 ModVolumeMultiplier（RitsuLib Mod 设置页滑杆 或 控制台命令
///    newsanguo_sfx_volume 调整，持久化保存）；
/// 4) “扎聋我自己的耳朵！”（deafen_me）的听觉受损门（音量降至 1/4），只作用于本播放器，
///    原版 FMOD 事件由 HearingVolumeController 另行降为 1/4。
/// </summary>
public static class NewsanguoSfx
{
    // 音频资源目录；事件名即为文件名
    private const string AudioDirectory = "res://newsanguo/audios/";

    // 全体音效总音量微调（dB）：在等响度均衡的基础上统一加减所有音效的音量。
    // 需要整体更响/更轻时改这里即可（如出牌音效偏轻可调到 +4 ~ +5）。
    // 2026-09-06 校准：默认 1.0× 与游戏其它音效相比整体偏轻约 10 dB（实测需调到 3.25× ≈ +10.2 dB 才正常），
    // 因此把基准从 +3 上调到 +13，使 1.0× 即为校准后的正常响度（倍率滑杆含义不变，仍可 0~4 微调）。
    // 2026-09-07 二次校准：实测默认仍偏轻，开到 2×（≈+6 dB）才正常听见，基准 +13 → +19。
    private const float MasterVolumeDb = 19f;

    // 依次尝试的音频扩展名
    private static readonly string[] AudioExtensions = [".mp3", ".wav", ".ogg"];

    // 已加载资源的缓存（key 为真实资源路径）
    private static readonly Dictionary<string, AudioStream> Cache = new();
    // 正在播放的播放器（按资源路径分组），便于静音/清理
    private static readonly Dictionary<string, List<AudioStreamPlayer>> ActivePlayers = new();
    // 已提示过缺失的事件名，避免每次播放都刷日志
    private static readonly HashSet<string> MissingReported = new();

    // 响度均衡增益表（单位 dB，播放时叠加到 VolumeDb）。
    // 自动生成：131 个音效，中位 RMS -22.8 dB，K=0.7（向中位靠拢），峰值余量 -3 dB。
    // 生成脚本见 %TEMP%\gen_sfx_gains.py（替换音频文件后重跑并替换本表）。
    // character_death / character_select / song_of_guan_yu 由引擎侧触发、不走响度测量，数值为手工保留。
    private static readonly Dictionary<string, float> LoudnessGainDb = new()
    {
        ["a_grand_toast"] = 1.5f,
        ["always_mine"] = -7.5f,
        ["bai_qi"] = 6.5f,
        ["better_each_day"] = 5f,
        ["better_than_yiling_flames"] = 7f,
        ["blade_of_virtue"] = -2f,
        ["blasphemy_debt"] = -2f,
        ["blood_loss"] = 5f,
        ["boneless_palm"] = -3f,
        ["brew_heals_all"] = 3.5f,
        ["brew_limit_break"] = -6.5f,
        ["caos_art_of_war"] = 3f,
        ["central_bastion"] = 2.5f,
        ["central_bastion_power"] = 2.5f,
        ["chain_stratagem1"] = -4f,
        ["chain_stratagem2"] = -2.5f,
        ["character_death"] = -2f,
        ["character_select"] = 2f,
        ["check_the_premiere"] = -1f,
        ["chenliu_mess_hall"] = -2.5f,
        ["chenliu_mess_hall_heal"] = 9.5f,
        ["chenliu_mess_hall_relic"] = 1f,
        ["commander_arrives"] = -3f,
        ["cricket_form"] = 4f,
        ["cricket_form_power"] = 4f,
        ["cross_for_cross"] = 6f,
        ["darkfin_shark"] = 5f,
        ["darkfin_shark_copy"] = 4.5f,
        ["deafen_me"] = -6.5f,
        ["defend_newsanguo"] = -3f,
        ["desecrate_heaven"] = -3.5f,
        ["divination"] = -4.5f,
        ["divine_insight"] = 7f,
        ["divine_insight_power"] = 6.5f,
        ["dong_zhuo_the_traitor"] = -3.5f,
        ["dragon_omen"] = 1.5f,
        ["empower"] = -5f,
        ["empower_power"] = -5f,
        ["fate_control"] = 2f,
        ["father_can_claim_the_throne"] = -3f,
        ["father_can_claim_the_throne_power"] = -3f,
        ["feel_no_acid"] = -2.5f,
        ["feel_no_acid_power"] = -6.5f,
        ["get_out"] = 1.5f,
        ["golden_rebellion"] = -1.5f,
        ["han_xin"] = 7f,
        ["heaven_and_earth"] = -3f,
        ["heaven_revision"] = 2f,
        ["heavenly_troops_power"] = 4.5f,
        ["heavens_decay"] = -4f,
        ["heavens_force"] = 9.5f,
        ["heavens_force_decay"] = 2f,
        ["human_transmutation_spell"] = -3f,
        ["im_getting_drunk"] = 1.5f,
        ["im_getting_drunk_power"] = 1.5f,
        ["intoxicated"] = 7f,
        ["invincible"] = -1.5f,
        ["invoke_heaven"] = -3.5f,
        ["just_kidding"] = -3.5f,
        ["lets_discuss"] = -4.5f,
        ["lightning_strike"] = -1.5f,
        ["loath_to_leave_the_table"] = -2f,
        ["loath_to_leave_the_table_damage"] = -2.5f,
        ["longevity_spell"] = -5f,
        ["mind_control_spell"] = -1.5f,
        ["my_three_generals"] = 8.5f,
        ["near_and_far"] = 3f,
        ["near_and_far_power"] = 4f,
        ["never_had_these"] = 4.5f,
        ["never_happened"] = -7f,
        ["new_game_plus"] = -3f,
        ["nonsense"] = -4f,
        ["off_with_your_head"] = -4.5f,
        ["off_with_your_head_double"] = -3f,
        ["one_man_stand"] = -6f,
        ["onset"] = 3f,
        ["party_on"] = 1.5f,
        ["peek_into_heaven"] = -3f,
        ["player_hurt"] = -3.5f,
        ["proxy_strike"] = -4f,
        ["qin_jin_alliance"] = -1.5f,
        ["quad_blast_1"] = 2f,
        ["quad_blast_2"] = 2.5f,
        ["quad_blast_3"] = 1.5f,
        ["quad_blast_4"] = 3.5f,
        ["reanimation_spell"] = -4.5f,
        ["release"] = 1.5f,
        ["retire"] = -1f,
        ["ruthless_blade"] = 4.5f,
        ["scorching_starfall"] = -3.5f,
        ["sea_change"] = 6.5f,
        ["self_fall"] = 5f,
        ["slam_the_bowl"] = -5.5f,
        ["slam_the_bowl_damage"] = -2.5f,
        ["smiling_tiger"] = 3.5f,
        ["smiling_tiger_copy"] = 3.5f,
        ["soldier"] = -2f,
        ["starry_night"] = -2.5f,
        ["strike_newsanguo"] = -2f,
        ["three_blades"] = 4f,
        ["to_a_bigger_goblet"] = 6f,
        ["tremble"] = 6.5f,
        ["triumph_brew"] = 10f,
        ["tweak"] = 10f,
        ["uncles_and_aunts"] = 4.5f,
        ["victory_by_heavens_will"] = 9.5f,
        ["victory_by_heavens_will_power"] = 9.5f,
        ["what_to_eat"] = -2.5f,
        ["where_s_wine"] = -4.5f,
        ["where_s_wine_power"] = -4.5f,
        ["who_rules"] = 7f,
        ["why_pick_that_up"] = -5f,
        ["wind_of_tiger"] = -2f,
        ["wind_of_tiger_power"] = -2.5f,
        ["wine_the_old_hero"] = 9f,
        ["wine_the_old_hero_power"] = 5.5f,
        ["zhou_yafu"] = 7.5f,
    };

    // 按音频文件名取响度补偿增益（dB）；未收录的文件视为 0
    private static float LoudnessOffsetDb(string resourcePath)
    {
        string stem = System.IO.Path.GetFileNameWithoutExtension(resourcePath);
        return LoudnessGainDb.TryGetValue(stem, out float gain) ? gain : 0f;
    }

    // —— 全局音量调节：跟随游戏“音效”音量滑杆 + 本 mod 独立倍率 ——
    //
    // 1) 游戏内“音效”音量（SettingsSave.VolumeSfx，0~1）：原生 FMOD 音效总线增益为“选项值²”，
    //    这里对 Godot 播放同步使用同一曲线（2×LinearToDb(option)），保证与原版音效同升同降。
    // 2) 本 mod 专属倍率：RitsuLib Mod 设置页的滑杆 或 游戏内控制台命令 newsanguo_sfx_volume <倍率>
    //    调整（默认 1.0），两者共用 ModVolumeMultiplier 同一真值源，持久化到
    //    user://newsanguo_sfx_volume.json，后续启动自动恢复。
    // 3) “听觉受损”门（deafen_me）在两者之上再乘 1/4，互不冲突。

    private const string VolumeConfigFileName = "newsanguo_sfx_volume.json";

    private static string VolumeConfigPath => System.IO.Path.Combine(OS.GetUserDataDir(), VolumeConfigFileName);

    // 配置文件字段：mod_volume（独立倍率，默认 1.0）+ sfx_enabled（总开关，默认开）。
    // 仅做一次磁盘读取，供下面两个静态字段初始化共用。
    private static readonly (float Multiplier, bool Enabled) _loadedConfig = LoadConfig();

    private static float _modVolumeMultiplier = _loadedConfig.Multiplier;

    private static float _modVolumeDb = ToDb(_modVolumeMultiplier);

    private static bool _sfxEnabled = _loadedConfig.Enabled;

    // 当前游戏“音效”音量选项对应的 dB 补偿（选项² 作为线性增益）
    private static float CurrentSfxOptionDb()
    {
        float option = SaveManager.Instance?.SettingsSave?.VolumeSfx ?? 0.5f;
        if (option <= 0f)
        {
            return -80f;
        }
        option = Mathf.Max(option, 0.0001f);
        return 2f * Mathf.LinearToDb(option);
    }

    /// <summary>本 mod 卡牌/能力音效的独立倍率（线性，默认 1.0；0 = 静音，上限 4）。</summary>
    public static float ModVolumeMultiplier
    {
        get => _modVolumeMultiplier;
        set
        {
            float clamped = Mathf.Clamp(value, 0f, 4f);
            if (Mathf.Abs(clamped - _modVolumeMultiplier) < 0.0001f)
            {
                return;
            }
            float newDb = ToDb(clamped);
            ShiftActiveVolumes(newDb - _modVolumeDb); // 已响起的声音也即时调整
            _modVolumeMultiplier = clamped;
            _modVolumeDb = newDb;
            SaveConfig();
        }
    }

    // 关闭总开关时对正在播放的音效一次性压低到不可闻的量（dB）；
    // 与 ToDb()/CurrentSfxOptionDb() 的 -80 静音约定一致，双向偏移可无损还原。
    private const float MuteShiftDb = 80f;

    /// <summary>本 mod 卡牌/能力音效总开关（默认开启；关闭后不再播放任何 mod 音效）。</summary>
    public static bool SfxEnabled
    {
        get => _sfxEnabled;
        set
        {
            if (_sfxEnabled == value)
            {
                return;
            }
            // 已响起的声音：关闭压低到静音，打开恢复（还原量 = MuteShiftDb）
            ShiftActiveVolumes(value ? MuteShiftDb : -MuteShiftDb);
            _sfxEnabled = value;
            SaveConfig();
        }
    }

    private static float ModVolumeOffsetDb() => _modVolumeDb;

    private static float ToDb(float linear) => linear <= 0f ? -80f : Mathf.LinearToDb(linear);

    // 读取磁盘配置（一次性）；文件缺失或损坏时退回默认（倍率 1.0、开关开）
    private static (float Multiplier, bool Enabled) LoadConfig()
    {
        float multiplier = 1f;
        bool enabled = true;
        try
        {
            string path = VolumeConfigPath;
            if (!System.IO.File.Exists(path))
            {
                return (multiplier, enabled);
            }
            using JsonDocument doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("mod_volume", out JsonElement volumeEl)
                && volumeEl.TryGetSingle(out float loaded))
            {
                multiplier = Mathf.Clamp(loaded, 0f, 4f);
            }
            // 只有显式 false 才算关闭；缺失/非布尔值一律按开启处理
            if (doc.RootElement.TryGetProperty("sfx_enabled", out JsonElement enabledEl))
            {
                enabled = enabledEl.ValueKind != JsonValueKind.False;
            }
        }
        catch
        {
            // 配置损坏时回退默认
        }
        return (multiplier, enabled);
    }

    // 把倍率 + 开关一起写盘（任一变化都会触发；写盘失败不影响本次会话内的音量）
    private static void SaveConfig()
    {
        try
        {
            string json = "{\"mod_volume\":"
                + _modVolumeMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ",\"sfx_enabled\":" + (_sfxEnabled ? "true" : "false") + "}";
            System.IO.File.WriteAllText(VolumeConfigPath, json);
        }
        catch
        {
            // 写盘失败不影响本次会话内的音量
        }
    }

    // 把当前倍率 + 开关重新写盘（RitsuLib 设置页 Save 委托复用；属性赋值时已即时保存，这里幂等）
    public static void SaveSfxConfig() => SaveConfig();

    // “听觉受损”门：置位后本 mod 的 Godot 音效整体降低 12 dB（线性音量 0.25），战斗结束/回主菜单时复位。
    private const float ReducedVolumeDb = 12.0412f; // 10*log10(4)，≈ 音量降至 1/4

    private static bool _volumeReduced;

    public static bool VolumeReduced => _volumeReduced;

    // 把本 mod 的全部 Godot 播放音效音量降至 1/4（已降则幂等跳过）
    public static void ApplyVolumeReduction()
    {
        if (_volumeReduced)
        {
            return;
        }
        _volumeReduced = true;
        ShiftActiveVolumes(-ReducedVolumeDb);
    }

    // 恢复本 mod 的 Godot 音效音量（未降则幂等跳过）
    public static void RestoreVolume()
    {
        if (!_volumeReduced)
        {
            return;
        }
        _volumeReduced = false;
        ShiftActiveVolumes(ReducedVolumeDb);
    }

    // 对所有正在播放的播放器统一增减 dB，让已响起的声音也立即变轻/恢复
    private static void ShiftActiveVolumes(float deltaDb)
    {
        foreach (List<AudioStreamPlayer> list in ActivePlayers.Values)
        {
            foreach (AudioStreamPlayer player in list)
            {
                if (GodotObject.IsInstanceValid(player))
                {
                    player.VolumeDb += deltaDb;
                }
            }
        }
    }

    // 把“事件路径 / res:// 路径”统一解析成资源与真实路径；解析不到时返回 false（已记录缺失日志）
    private static bool ResolveAudio(string sfx, out AudioStream? stream, out string resourcePath)
    {
        stream = null;
        resourcePath = string.Empty;
        // 事件路径 → 在音频目录下按候选扩展名找资源；res:// 路径原样使用
        string? eventName = GetEventName(sfx);
        return eventName is not null
            ? TryLoadResource(eventName, sfx, out stream, out resourcePath)
            : TryLoadResource(sfx, sfx, out stream, out resourcePath);
    }

    /// <summary>
    /// 播放一次音效（不等待播放完毕）。返回正在播放的播放器，便于"先播放、稍后再等它结束"的节奏场景。
    /// </summary>
    /// <param name="sfx">兼容两种传参：事件路径（event:/newsanguo/sfx/xxx）或 res:// 资源路径。</param>
    /// <returns>正在播放的 AudioStreamPlayer；总开关关闭或资源缺失/解析失败时为 null（不播放）。</returns>
    public static AudioStreamPlayer? Play(string sfx, float volume = 1f, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(sfx))
        {
            return null;
        }
        // 总开关关闭时不播放任何 mod 音效（已响起的声音由 SfxEnabled setter 即时压静音）
        if (!_sfxEnabled)
        {
            return null;
        }

        if (ResolveAudio(sfx, out AudioStream? stream, out string resourcePath) && stream is not null)
        {
            return PlayStream(resourcePath, stream, volume, pitch);
        }
        return null;
    }

    /// <summary>
    /// 播放一次音效并等待它自然播放完毕后再返回。
    /// 供“播放 → 等待自然结束 → 再继续下一步”这类需要卡点节奏的效果使用；
    /// 总开关关闭或资源缺失时立即返回（不播放、不等待）。
    /// </summary>
    /// <param name="sfx">兼容两种传参：事件路径（event:/newsanguo/sfx/xxx）或 res:// 资源路径。</param>
    public static async Task PlayAndWaitAsync(string sfx, float volume = 1f, float pitch = 1f)
    {
        AudioStreamPlayer? player = Play(sfx, volume, pitch);
        await WaitFinishedAsync(player);
    }

    /// <summary>
    /// 等待一个已开始播放的音效自然播放完毕后再返回。
    /// 供“先播放（不等待）→ 处理其它效果 → 在进入下一段前再等它播完”的场景使用
    /// （如分段连击：每段先播音效再结算，段间 await 本方法即可保证各段语音不重叠）。
    /// 播放器为 null、已失效或已播放完毕时立即返回，避免永久等待。
    /// </summary>
    /// <param name="player">由 <see cref="Play"/> 返回、正在播放的播放器。</param>
    public static async Task WaitFinishedAsync(AudioStreamPlayer? player)
    {
        if (player is null || !GodotObject.IsInstanceValid(player))
        {
            return;
        }

        TaskCompletionSource finished = new();
        player.Finished += () => finished.TrySetResult();
        // 极短音效可能在挂上 Finished 前就已播完：此时直接放行，避免永久等待
        if (!player.Playing)
        {
            finished.TrySetResult();
        }
        await finished.Task;
    }

    private static AudioStreamPlayer? PlayStream(string resourcePath, AudioStream stream, float volume, float pitch)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return null;
        }

        AudioStreamPlayer player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = "SFX",
            PitchScale = pitch,
            VolumeDb = volume <= 0f ? -80f
                : Mathf.LinearToDb(volume) + MasterVolumeDb + LoudnessOffsetDb(resourcePath)
                    + CurrentSfxOptionDb() + ModVolumeOffsetDb() + (_volumeReduced ? -ReducedVolumeDb : 0f)
        };
        tree.Root.AddChild(player);
        if (!ActivePlayers.TryGetValue(resourcePath, out var list))
        {
            list = [];
            ActivePlayers[resourcePath] = list;
        }
        list.Add(player);
        player.Play();

        // 播放完毕自动清理
        player.Finished += () => FinishAndFree(resourcePath, player);
        return player;
    }

    // 依次尝试 .mp3/.wav/.ogg；mp3 额外用原始文件读取兜底（兼容未导入的裸 mp3）
    private static bool TryLoadResource(string pathOrEvent, string originalSfx, out AudioStream? stream, out string resourcePath)
    {
        if (!pathOrEvent.StartsWith("res://"))
        {
            // 事件路径：在音频目录内探测扩展名
            string basePath = AudioDirectory + pathOrEvent;
            foreach (string ext in AudioExtensions)
            {
                string candidate = basePath + ext;
                if (TryLoadSingle(candidate, out stream))
                {
                    resourcePath = candidate;
                    return true;
                }
            }

            if (MissingReported.Add(pathOrEvent))
            {
                GD.PrintErr($"NewsanguoSfx: audio resource not found for {originalSfx} (tried {AudioDirectory}{pathOrEvent}.mp3/.wav/.ogg)");
            }
            stream = null;
            resourcePath = basePath;
            return false;
        }

        // res:// 路径：按给定扩展名加载
        if (TryLoadSingle(pathOrEvent, out stream))
        {
            resourcePath = pathOrEvent;
            return true;
        }

        if (MissingReported.Add(pathOrEvent))
        {
            GD.PrintErr($"NewsanguoSfx: audio resource not found for {pathOrEvent}");
        }
        resourcePath = pathOrEvent;
        return false;
    }

    // 依次尝试 .mp3/.wav/.ogg；导入过的资源走 ResourceLoader，裸文件（未导入、仅被打进 pck）则按原始文件读取
    private static bool TryLoadSingle(string path, out AudioStream? stream)
    {
        if (Cache.TryGetValue(path, out stream))
        {
            return true;
        }

        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (Godot.FileAccess.FileExists(path))
        {
            // 裸文件存在（pck 里的原始音频，未经 Godot 导入）：直接按原始文件读取。
            // mp3 由内建解码器读取；wav 需手工解析成 AudioStreamWav（运行时无原生 wav 加载器）。
            stream = ext switch
            {
                ".mp3" => AudioStreamMP3.LoadFromFile(path),
                ".wav" => LoadRawWav(path),
                _ => ResourceLoader.Load<AudioStream>(path)
            };
        }
        else
        {
            // 裸文件不存在：说明已被编辑器导入（res:// 路径 remap 到 .godot/imported），走资源系统
            stream = ResourceLoader.Load<AudioStream>(path);
        }
        if (stream is null)
        {
            return false;
        }

        Cache[path] = stream;
        return true;
    }

    // 解析裸 WAV（RIFF 未压缩 PCM），不依赖 Godot 编辑器的导入管线。
    // Godot 运行时没有针对 .wav 的原生资源加载器，未被导入的 wav 必须手工解码成 AudioStreamWav。
    private static AudioStreamWav? LoadRawWav(string path)
    {
        byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes is null || bytes.Length < 44)
        {
            return null;
        }

        // RIFF / WAVE 魔数
        if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F' ||
            bytes[8] != (byte)'W' || bytes[9] != (byte)'A' || bytes[10] != (byte)'V' || bytes[11] != (byte)'E')
        {
            return null;
        }

        int sampleRate = 0;
        int bitsPerSample = 16;
        int channels = 1;
        ushort formatTag = 0;
        int dataStart = -1;
        int dataLength = 0;

        // 遍历子块，定位 fmt 与 data
        int pos = 12;
        while (pos + 8 <= bytes.Length)
        {
            uint chunkId = BitConverter.ToUInt32(bytes, pos);
            int chunkSize = BitConverter.ToInt32(bytes, pos + 4);
            int payloadStart = pos + 8;
            if (payloadStart + chunkSize > bytes.Length)
            {
                return null;
            }

            switch (chunkId)
            {
                case 0x20746D66u: // "fmt "
                    if (chunkSize >= 16)
                    {
                        formatTag = BitConverter.ToUInt16(bytes, payloadStart);
                        channels = BitConverter.ToUInt16(bytes, payloadStart + 2);
                        sampleRate = BitConverter.ToInt32(bytes, payloadStart + 4);
                        bitsPerSample = BitConverter.ToUInt16(bytes, payloadStart + 14);
                    }
                    break;
                case 0x61746164u: // "data"
                    dataStart = payloadStart;
                    dataLength = chunkSize;
                    break;
            }

            if (chunkId == 0x61746164u)
            {
                break; // data 块之后的都是尾部元数据
            }
            pos = payloadStart + chunkSize + (chunkSize & 1);
        }

        // 仅支持未压缩 PCM（8/16 位）。FMOD 导出的音效即 16 位 PCM；Godot 的 AudioStreamWav
        // 只支持 8/16 位 PCM（另有 ADPCM/QOA），不支持 32 位 float 与压缩格式。
        if (formatTag != 1 || bitsPerSample is not (8 or 16) ||
            sampleRate <= 0 || dataStart < 0 || dataLength <= 0)
        {
            return null;
        }

        byte[] data = new byte[dataLength];
        Array.Copy(bytes, dataStart, data, 0, dataLength);

        return new AudioStreamWav
        {
            Data = data,
            Format = bitsPerSample == 8
                ? AudioStreamWav.FormatEnum.Format8Bits
                : AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = channels == 2,
            LoopMode = AudioStreamWav.LoopModeEnum.Disabled
        };
    }

    // 从事件路径里取出末段作为文件名；res:// 路径返回 null
    private static string? GetEventName(string sfx)
    {
        if (sfx.StartsWith("res://"))
        {
            return null;
        }
        int lastSlash = sfx.LastIndexOf('/');
        return lastSlash >= 0 ? sfx.Substring(lastSlash + 1) : sfx;
    }

    private static void FinishAndFree(string resourcePath, AudioStreamPlayer player)
    {
        if (ActivePlayers.TryGetValue(resourcePath, out var list))
        {
            list.Remove(player);
            if (list.Count == 0)
            {
                ActivePlayers.Remove(resourcePath);
            }
        }
        if (GodotObject.IsInstanceValid(player))
        {
            player.QueueFree();
        }
    }
}
