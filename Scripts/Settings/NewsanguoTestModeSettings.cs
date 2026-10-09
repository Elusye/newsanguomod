using System.Text.Json;
using Godot;
using STS2RitsuLib.Settings;

namespace newsanguo.Scripts.Settings;

/// <summary>
/// “测试模式”开关（默认关闭），持久化到 user://newsanguo_test_mode.json。
///
/// 用途：测试中的内容只在开启测试模式时才对玩家可见可选。当前唯一受它控制的是“蜀汉”角色：
/// 关闭时蜀汉从原版角色选择界面隐藏、也不参与“随机角色”（见 ShuHanCharacter 里对
/// RitsuLib IModCharacterVanillaSelectionPolicy 属性的覆写）。
///
/// 与 NewsanguoSfx 的音效配置分两个文件各存一份：音效那份是运行期调音用的，
/// 这里是内容可见性开关，分开写更直观（都在游戏 user 数据目录下，便于手动编辑/删除）。
/// </summary>
public static class NewsanguoTestModeSettings
{
    private const string ModId = Entry.ModId;

    // RitsuLib 设置页控件的数据键（设置页内部读写用，与落盘文件名无关）
    private const string DataKey = "test_mode_enabled";

    private const string ConfigFileName = "newsanguo_test_mode.json";

    private static string ConfigPath => System.IO.Path.Combine(OS.GetUserDataDir(), ConfigFileName);

    // 只读一次盘，供下面的静态字段初始化
    private static readonly bool LoadedEnabled = LoadTestMode();

    private static bool _testModeEnabled = LoadedEnabled;

    /// <summary>
    /// 测试模式开关（默认关闭）。关闭时“蜀汉”角色在角色选择界面隐藏，且不会被“随机角色”选中。
    /// 赋值即时写盘；开关变化后重新进入角色选择界面即可生效（无需重启游戏）。
    /// </summary>
    public static bool TestModeEnabled
    {
        get => _testModeEnabled;
        set
        {
            if (_testModeEnabled == value)
            {
                return;
            }
            _testModeEnabled = value;
            SaveConfig();
        }
    }

    /// <summary>
    /// 创建设置页里的“测试模式”开关绑定：与 <see cref="TestModeEnabled"/> 共用同一个真值源，
    /// 因此控制台/配置文件改动与设置页开关互相可见，不会各存一份。
    /// </summary>
    public static IModSettingsValueBinding<bool> CreateTestModeBinding()
    {
        return ModSettingsBindings.Callback<bool>(
            ModId,
            DataKey,
            read: () => TestModeEnabled,
            write: value => TestModeEnabled = value,
            save: SaveTestModeConfig);
    }

    // 读盘：文件缺失、损坏、字段缺失或非 true 一律按关闭处理（测试模式默认不开）
    private static bool LoadTestMode()
    {
        try
        {
            string path = ConfigPath;
            if (!System.IO.File.Exists(path))
            {
                return false;
            }
            using JsonDocument doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("test_mode_enabled", out JsonElement el)
                && el.ValueKind == JsonValueKind.True;
        }
        catch
        {
            return false;
        }
    }

    private static void SaveConfig()
    {
        try
        {
            System.IO.File.WriteAllText(
                ConfigPath,
                "{\"test_mode_enabled\":" + (_testModeEnabled ? "true" : "false") + "}");
        }
        catch
        {
            // 写盘失败不影响本次会话内的开关状态
        }
    }

    // 供设置页 Save 委托复用（属性赋值时已即时保存，这里幂等）
    public static void SaveTestModeConfig() => SaveConfig();
}
