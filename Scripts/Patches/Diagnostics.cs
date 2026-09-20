using System;
using System.IO;

namespace newsanguo.Scripts.Patches;

// 诊断日志：写入 %TEMP%\newsanguo_diagnostics.log，用于确认补丁挂载与触发情况。
// 游戏 Logger 走 stdout（控制台），不落文件，联机/正常启动时看不到，因此这里单独落盘。
//
// 注：早期为排查“能量图标/描述字号自动缩放”而挂在
// MegaRichTextLabel.SetTextAutoSize / MegaLabelHelper.EstimateTextSize / MegaRichTextLabel.AdjustFontSize
// 上的三个诊断补丁已删除——它们位于文本渲染热路径，每次渲染都会跑前缀/后缀并写日志，
// 问题解决后只剩无谓开销（其中 AccessTools 反射读私有字段那处还会触发 CS8605 警告）。
public static class Diagnostics
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "newsanguo_diagnostics.log");

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{System.Environment.NewLine}");
        }
        catch
        {
            // 写日志失败不影响游戏功能
        }
    }
}
