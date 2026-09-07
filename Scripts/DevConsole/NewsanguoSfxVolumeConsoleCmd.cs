using System;
using System.Globalization;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

using newsanguo.Scripts;

namespace newsanguo.Scripts.DevConsole;

/// <summary>
/// 游戏内控制台命令：开关并调整新三国卡牌/能力音效的独立音量倍率。
///
/// 说明：DevConsole 会自动扫描所有 mod 程序集里 AbstractConsoleCmd 的子类并注册命令，
/// 因此本类不需要手动注册。音量计算 = 游戏“音效”滑杆 × 本倍率 × 听觉受损门（若有）。
/// 用法：
///   newsanguo_sfx_volume          —— 显示当前开关与倍率；
///   newsanguo_sfx_volume on|off   —— 打开/关闭本 mod 音效（与设置菜单的“启用卡牌音效”同源）；
///   newsanguo_sfx_volume <倍率>   —— 设置倍率（1.0 恢复默认；0 静音；上限 4.0）。
/// </summary>
public class NewsanguoSfxVolumeConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "newsanguo_sfx_volume";

    public override string Args => "on|off|<multiplier>";

    public override string Description => "Enable/disable or adjust the newsanguo card/ability SFX volume multiplier (default 1.0; game SFX slider still applies). No arg = show current.";

    // 本地（非多人同步）的音量偏好，不用走网络
    public override bool IsNetworked => false;

    // 普通玩家也能使用该音量命令
    public override bool DebugOnly => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0)
        {
            return new CmdResult(
                success: true,
                $"newsanguo card SFX: enabled = {NewsanguoSfx.SfxEnabled}, volume multiplier = "
                + $"{NewsanguoSfx.ModVolumeMultiplier.ToString("0.###", CultureInfo.InvariantCulture)}. "
                + "Usage: newsanguo_sfx_volume <on|off|multiplier> (1.0 = default).");
        }

        if (args[0].Equals("on", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("enable", StringComparison.OrdinalIgnoreCase))
        {
            NewsanguoSfx.SfxEnabled = true;
            return new CmdResult(success: true, "newsanguo card SFX enabled.");
        }

        if (args[0].Equals("off", StringComparison.OrdinalIgnoreCase)
            || args[0].Equals("disable", StringComparison.OrdinalIgnoreCase))
        {
            NewsanguoSfx.SfxEnabled = false;
            return new CmdResult(success: true, "newsanguo card SFX disabled.");
        }

        if (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float multiplier)
            || float.IsNaN(multiplier) || float.IsInfinity(multiplier))
        {
            return new CmdResult(success: false, "Invalid argument. Usage: newsanguo_sfx_volume <on|off|multiplier> (1.0 = default).");
        }

        NewsanguoSfx.ModVolumeMultiplier = multiplier;
        return new CmdResult(
            success: true,
            $"newsanguo card SFX volume multiplier set to {NewsanguoSfx.ModVolumeMultiplier.ToString("0.###", CultureInfo.InvariantCulture)} "
            + "(game SFX slider still applies; use 1.0 to reset, 0 to mute, up to 4.0).");
    }
}
