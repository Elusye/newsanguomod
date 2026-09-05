using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace newsanguo.Scripts;

/// <summary>
/// 全 mod 音效统一播放入口。
///
/// 说明：音效已全面脱离 FMOD——卡牌/能力/事件直接经这里用 Godot AudioStreamPlayer 播放
/// res://newsanguo/audios/ 下的音频资源（支持 .mp3/.wav/.ogg，自动探测扩展名，与“关羽之歌”同款做法）。
/// 角色选人/死亡两个由引擎触发的 FMOD 事件，则经 Entry 注册为 RitsuLib 虚拟事件后同样映射到同名资源。
///
/// “扎聋我自己的耳朵！”（deafen_me）的音量减半只作用于本播放器（Godot 播放），
/// 原版 FMOD 事件由 HearingVolumeController 另行减半。
/// </summary>
public static class NewsanguoSfx
{
    // 音频资源目录；事件名即为文件名
    private const string AudioDirectory = "res://newsanguo/audios/";

    // 全体音效总音量微调（dB）：在等响度均衡的基础上统一加减所有音效的音量。
    // 需要整体更响/更轻时改这里即可（如出牌音效偏轻可调到 +4 ~ +5）。
    private const float MasterVolumeDb = 3f;

    // 依次尝试的音频扩展名
    private static readonly string[] AudioExtensions = [".mp3", ".wav", ".ogg"];

    // 已加载资源的缓存（key 为真实资源路径）
    private static readonly Dictionary<string, AudioStream> Cache = new();
    // 正在播放的播放器（按资源路径分组），便于静音/清理
    private static readonly Dictionary<string, List<AudioStreamPlayer>> ActivePlayers = new();
    // 已提示过缺失的事件名，避免每次播放都刷日志
    private static readonly HashSet<string> MissingReported = new();

    // 响度均衡增益表（单位 dB，播放时叠加到 VolumeDb）。
    // 由响度测量脚本（分析 RMS/峰值，中位 RMS -22.7 dB 为基准、按 70% 向中位靠拢、峰值留 -3 dB 余量防削波）
    // 对 newsanguo/audios 下全部音效自动生成；替换音频文件后需重新测量更新本表。
    private static readonly Dictionary<string, float> LoudnessGainDb = new()
    {
        ["a_grand_toast"] = 1.5f,
        ["always_mine"] = -7.5f,
        ["bai_qi"] = 7f,
        ["better_each_day"] = 5.5f,
        ["better_than_yiling_flames"] = 7.5f,
        ["blade_of_virtue"] = -2f,
        ["blasphemy_debt"] = -2f,
        ["blood_loss"] = 5f,
        ["boneless_palm"] = -3f,
        ["brew_heals_all"] = 3.5f,
        ["brew_limit_break"] = -6.5f,
        ["central_bastion"] = 3f,
        ["central_bastion_power"] = 2.5f,
        ["chain_stratagem1"] = -4f,
        ["chain_stratagem2"] = -2.5f,
        ["character_death"] = -2f,
        ["character_select"] = 2f,
        ["check_the_premiere"] = -1f,
        ["chenliu_mess_hall"] = -2f,
        ["chenliu_mess_hall_heal"] = 9.5f,
        ["chenliu_mess_hall_relic"] = 1f,
        ["commander_arrives"] = -3f,
        ["cricket_form"] = 4f,
        ["cricket_form_power"] = 4f,
        ["cross_for_cross"] = 6f,
        ["darkfin_shark"] = 5f,
        ["darkfin_shark_copy"] = 5f,
        ["deafen_me"] = -6.5f,
        ["defend_newsanguo"] = -3f,
        ["desecrate_heaven"] = -3.5f,
        ["divination"] = -4.5f,
        ["divine_insight"] = 7f,
        ["divine_insight_power"] = 6.5f,
        ["dong_zhuo_the_traitor"] = -3f,
        ["dragon_omen"] = 2f,
        ["empower"] = -4.5f,
        ["empower_power"] = -4.5f,
        ["fate_control"] = 2f,
        ["fate_unknown"] = -1f,
        ["father_can_claim_the_throne"] = -2.5f,
        ["father_can_claim_the_throne_power"] = -2.5f,
        ["feel_no_acid"] = -2.5f,
        ["feel_no_acid_power"] = -6.5f,
        ["get_out"] = 2f,
        ["golden_rebellion"] = -1.5f,
        ["han_xin"] = 7.5f,
        ["heaven_and_earth"] = -3f,
        ["heaven_revision"] = 2f,
        ["heavenly_troops_power"] = 4.5f,
        ["heavens_decay"] = -4f,
        ["heavens_force"] = 9.5f,
        ["heavens_force_decay"] = 2f,
        ["human_transmutation_spell"] = -3f,
        ["infinite_camps"] = -3f,
        ["intoxicated"] = 7f,
        ["invincible"] = -1f,
        ["invoke_heaven"] = -3f,
        ["just_kidding"] = -3f,
        ["lets_discuss"] = -4.5f,
        ["lightning_strike"] = -1.5f,
        ["loath_to_leave_the_table"] = -2f,
        ["loath_to_leave_the_table_damage"] = -2.5f,
        ["longevity_spell"] = -4.5f,
        ["mind_control_spell"] = -1f,
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
        ["onset"] = 3.5f,
        ["party_on"] = 2f,
        ["peek_into_heaven"] = -3f,
        ["player_hurt"] = -2f,
        ["proxy_strike"] = -4f,
        ["qin_jin_alliance"] = -1f,
        ["quad_blast"] = 4.5f,
        ["reanimation_spell"] = -4.5f,
        ["release"] = 1.5f,
        ["retire"] = -1f,
        ["ruthless_blade"] = 5f,
        ["scorching_starfall"] = -3f,
        ["sea_change"] = 6.5f,
        ["self_fall"] = 5.5f,
        ["slam_the_bowl"] = -5.5f,
        ["slam_the_bowl_damage"] = -2.5f,
        ["smiling_tiger"] = 4f,
        ["smiling_tiger_copy"] = 3.5f,
        ["soldier"] = -2f,
        ["starry_night"] = -2f,
        ["strike_newsanguo"] = -2f,
        ["three_blades"] = 4f,
        ["to_a_bigger_goblet"] = 6f,
        ["tremble"] = 6.5f,
        ["triumph_brew"] = 10f,
        ["tweak"] = 10f,
        ["uncles_and_aunts"] = 4.5f,
        ["victory_by_heavens_will"] = 9.5f,
        ["victory_by_heavens_will_power"] = 9.5f,
        ["what_to_eat"] = -2f,
        ["where_s_wine"] = -4.5f,
        ["where_s_wine_power"] = -4.5f,
        ["who_rules"] = 7f,
        ["wind_of_tiger"] = -1.5f,
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

    /// <summary>
    /// 播放一次音效。
    /// </summary>
    /// <param name="sfx">兼容两种传参：事件路径（event:/newsanguo/sfx/xxx）或 res:// 资源路径。</param>
    public static void Play(string sfx, float volume = 1f, float pitch = 1f)
    {
        if (string.IsNullOrEmpty(sfx))
        {
            return;
        }

        // 事件路径 → 在音频目录下按候选扩展名找资源；res:// 路径原样使用
        string? eventName = GetEventName(sfx);
        if (eventName is not null)
        {
            TryLoadResource(eventName, sfx, out AudioStream? stream, out string resourcePath);
            if (stream is null)
            {
                return; // 缺失：已记录日志
            }
            PlayStream(resourcePath, stream, volume, pitch);
            return;
        }

        // 直接传 res:// 资源路径
        TryLoadResource(sfx, sfx, out AudioStream? directStream, out string directPath);
        if (directStream is not null)
        {
            PlayStream(directPath, directStream, volume, pitch);
        }
    }

    private static void PlayStream(string resourcePath, AudioStream stream, float volume, float pitch)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        AudioStreamPlayer player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = "SFX",
            PitchScale = pitch,
            VolumeDb = volume <= 0f ? -80f : Mathf.LinearToDb(volume) + MasterVolumeDb + LoudnessOffsetDb(resourcePath)
                + (_volumeReduced ? -ReducedVolumeDb : 0f)
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
