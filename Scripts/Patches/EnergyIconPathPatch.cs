using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Text;

namespace newsanguo.Scripts.Patches;

// 能量图标相关逻辑已迁移到 RitsuLib 官方 API（NewsanguoCardPool.BigEnergyIconPath / TextEnergyIconPath），
// 本文件仅保留针对能量图标文本的诊断补丁，用于排查描述渲染/自动缩放问题。

// 诊断补丁：记录最终进入卡牌描述标签的完整 bbcode 文本。
// 目的：确认 formatter 输出的 [img] 标签是否完好无损地到达渲染层，
// 以及途中是否被追加/改写（比如 SmartFormat 在 formatter 返回 false 后追加默认格式化结果）。
[HarmonyPatch(typeof(MegaRichTextLabel), nameof(MegaRichTextLabel.SetTextAutoSize))]
public static class SetTextAutoSizeDiagnosticPatch
{
    private static int _logCount;

    public static void Prefix(MegaRichTextLabel __instance, string text)
    {
        // 只记录与能量图标相关的文本，控制日志量
        if (text.Contains("newsanguo") || text.Contains("energy_icon") || text.Contains("[img]"))
        {
            if (_logCount >= 40)
            {
                return;
            }
            _logCount++;
            Diagnostics.Log($"[SetTextAutoSize] 节点={__instance.GetPath()} 文本={text.Replace("\n", "\\n")}");
        }
    }
}

// 诊断补丁：记录自动缩放对含能量图标文本的测量尺寸。
// 关键点：测量时用 texture2D.GetSize()（忽略 [img] 的 width/height），若此处尺寸异常，
// 会导致自动缩放选错字号、文字溢出卡框。
[HarmonyPatch(typeof(MegaLabelHelper), nameof(MegaLabelHelper.EstimateTextSize),
    new[] { typeof(TextParagraph), typeof(List<BbcodeObject>), typeof(Font), typeof(int), typeof(float), typeof(float) })]
public static class EstimateTextSizeDiagnosticPatch
{
    private static int _logCount;

    public static void Postfix(List<BbcodeObject> objs, int fontSize, Vector2 __result)
    {
        if (_logCount >= 60)
        {
            return;
        }
        try
        {
            if (objs.Any(o => o.text != null && o.text.Contains("energy_newsanguo")))
            {
                _logCount++;
                Diagnostics.Log($"[EstimateTextSize] 含能量图标 font={fontSize} 测量={__result}");
            }
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[EstimateTextSize] 诊断异常: {e.Message}");
        }
    }
}

// 诊断补丁：记录描述标签自动缩放最终选定的字号（含能量图标的标签）。
// AdjustFontSize 是私有方法，nameof 无法引用，这里用字符串名让 Harmony 在运行时通过反射解析。
// 字段 _lastSetSize 同样用反射读取（___ 注入在当前 Harmony 版本下无法匹配带下划线的字段名）。
[HarmonyPatch(typeof(MegaRichTextLabel), "AdjustFontSize")]
public static class AdjustFontSizeDiagnosticPatch
{
    private static int _logCount;

    public static void Postfix(MegaRichTextLabel __instance)
    {
        if (_logCount >= 60)
        {
            return;
        }
        try
        {
            string text = __instance.Text;
            if (text != null && text.Contains("energy_newsanguo"))
            {
                _logCount++;
                int lastSetSize = AccessTools.Field(typeof(MegaRichTextLabel), "_lastSetSize") is { } field
                    ? (int)field.GetValue(__instance)
                    : -1;
                Diagnostics.Log($"[AdjustFontSize] 节点={__instance.GetPath()} 字号={lastSetSize} 标签Size={__instance.Size} AutoSize={__instance.AutoSizeEnabled} 文本长度={text.Length}");
            }
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[AdjustFontSize] 诊断异常: {e.Message}");
        }
    }
}

// 诊断日志：写入 %TEMP%\newsanguo_diagnostics.log，用于确认补丁挂载与触发情况。
// 游戏 Logger 走 stdout（控制台），不落文件，联机/正常启动时看不到，因此这里单独落盘。
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
