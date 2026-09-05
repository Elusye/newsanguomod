using System;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;

namespace newsanguo.Scripts;

/// <summary>
/// “扎聋我自己的耳朵！”负面效果的统一音量控制：
/// 战斗中把本机听到的声音整体降到 25%（约 -12 dB），战斗结束 / SL / 回主菜单时恢复原音量。
/// 覆盖互不影响的音频来源：
/// 1) 引擎 FMOD 音效/音乐/环境声 → 通过降低游戏主总线音量（NAudioManager.SetMasterVol）；
/// 2) 本 mod 自己的 Godot 播放音效 → NewsanguoSfx.ApplyVolumeReduction()。
/// 只在打出卡牌的玩家本机生效（由调用方在 LocalContext.IsMe 下调用），整体幂等，重复调用/兜底恢复均安全。
/// </summary>
public static class HearingVolumeController
{
    // 目标音量：线性振幅 0.25（约 -12 dB，听感约为原来的 1/4）
    private const float TargetLinearVolume = 0.25f;

    // 是否正处于音量降低状态
    private static bool _reduced;

    public static bool IsReduced => _reduced;

    /// <summary>把本机听到的声音音量降低到原来的 25%（幂等）。</summary>
    public static void ReduceToQuarterVolume()
    {
        if (_reduced)
        {
            return;
        }
        _reduced = true;
        ApplyFmodMasterScale(TargetLinearVolume);
        NewsanguoSfx.ApplyVolumeReduction();
    }

    /// <summary>恢复本机声音到原音量（幂等）。</summary>
    public static void RestoreFullVolume()
    {
        if (!_reduced)
        {
            return;
        }
        _reduced = false;
        ApplyFmodMasterScale(1f);
        NewsanguoSfx.RestoreVolume();
    }

    // 游戏把音量选项（0~1）平方后写入 FMOD 主总线（bus = option²），
    // 所以要让总线音量乘以 scale，需把选项值乘以 sqrt(scale)。
    // 恢复时用当前存档选项值重新写回，保证与用户实际设置一致（引擎重开设置/启动会覆盖，兜底恢复每次都重写）。
    private static void ApplyFmodMasterScale(float scale)
    {
        NAudioManager? audio = NAudioManager.Instance;
        if (audio is null)
        {
            return;
        }

        float option = SaveManager.Instance?.SettingsSave?.VolumeMaster ?? 0.5f;
        audio.SetMasterVol(option * MathF.Sqrt(scale));
    }
}
