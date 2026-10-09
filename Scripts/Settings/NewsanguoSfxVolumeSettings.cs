using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace newsanguo.Scripts.Settings;

/// <summary>
/// 把本 mod 的设置项注册为 RitsuLib 的 Mod 设置页（游戏内“设置 → Mod 设置”）。
/// 当前含两个分区：“音效音量”（卡牌/能力音效开关与倍率）与“测试模式”
/// （测试中的内容是否可见，当前控制“蜀汉”角色是否出现在角色选择界面，见 <see cref="NewsanguoTestModeSettings"/>）。
///
/// 与控制台命令 newsanguo_sfx_volume 共用同一个持久化真值源（NewsanguoSfx.SfxEnabled /
/// NewsanguoSfx.ModVolumeMultiplier）：设置页控件的 Read/Write 直接读写这些属性
/// （Write 时属性 setter 会即时调音量并写盘），因此两处改动互相可见，不会出现各存一份的问题。
/// “打击/防御/士兵/灵魂锁链”出牌音效开关（NewsanguoSfx.BasicCardSfxEnabled）同样持久化到该文件。
/// </summary>
public static class NewsanguoSfxVolumeSettings
{
    private const string ModId = Entry.ModId;

    private const string EnabledDataKey = "card_sfx_enabled";

    private const string VolumeDataKey = "card_sfx_volume_multiplier";

    private const string BasicCardDataKey = "basic_card_sfx_enabled";

    public static void Register()
    {
        RitsuLibFramework.RegisterModSettings(ModId, page =>
        {
            page.WithSortOrder(0)
                .WithModDisplayName(ModSettingsText.Literal("新三国"))
                .WithTitle(ModSettingsText.Literal("新三国设置"))
                .WithDescription(ModSettingsText.Literal(
                    "新三国 mod 的通用设置：卡牌/能力音效、测试模式。"))
                .AddSection("sfx_volume", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("音效音量"))
                        .AddToggle(
                            id: "card_sfx_enabled",
                            label: ModSettingsText.Literal("启用卡牌音效"),
                            binding: CreateEnabledBinding(),
                            description: ModSettingsText.Literal(
                                "关闭后本 mod 的卡牌/能力/事件音效全部不播放（游戏其它音效不受影响）。"))
                        .AddToggle(
                            id: "basic_card_sfx_enabled",
                            label: ModSettingsText.Literal("启用三国杀出牌音效"),
                            binding: CreateBasicCardBinding(),
                            description: ModSettingsText.Literal(
                                "关闭后“打击”“防御”“士兵”“灵魂锁链”的出牌音效不播放，其它音效不受影响。"))
                        .AddSlider(
                            id: "card_sfx_volume_multiplier",
                            label: ModSettingsText.Literal("卡牌音效倍率"),
                            binding: CreateVolumeBinding(),
                            minValue: 0,
                            maxValue: 4,
                            step: 0.05,
                            valueFormatter: value => value.ToString("0.##") + "×",
                            description: ModSettingsText.Literal(
                                "1× = 默认音量；0 = 静音；最高 4×。也可用控制台命令 newsanguo_sfx_volume 调整。"));
                })
                .AddSection("test_mode", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("测试模式"))
                        .AddToggle(
                            id: "test_mode_enabled",
                            label: ModSettingsText.Literal("测试模式"),
                            binding: NewsanguoTestModeSettings.CreateTestModeBinding(),
                            description: ModSettingsText.Literal(
                                "开启后“蜀汉”角色才会出现在角色选择界面；关闭时隐藏，且不会被“随机角色”选中。"
                                + "用于测试中的内容。修改后重新进入角色选择界面即可生效，无需重启游戏。"));
                });
        });
    }

    private static IModSettingsValueBinding<bool> CreateEnabledBinding()
    {
        return ModSettingsBindings.Callback<bool>(
            ModId,
            EnabledDataKey,
            read: () => NewsanguoSfx.SfxEnabled,
            write: value => NewsanguoSfx.SfxEnabled = value,
            save: () => NewsanguoSfx.SaveSfxConfig());
    }

    private static IModSettingsValueBinding<bool> CreateBasicCardBinding()
    {
        return ModSettingsBindings.Callback<bool>(
            ModId,
            BasicCardDataKey,
            read: () => NewsanguoSfx.BasicCardSfxEnabled,
            write: value => NewsanguoSfx.BasicCardSfxEnabled = value,
            save: () => NewsanguoSfx.SaveSfxConfig());
    }

    private static IModSettingsValueBinding<double> CreateVolumeBinding()
    {
        return ModSettingsBindings.Callback<double>(
            ModId,
            VolumeDataKey,
            read: () => NewsanguoSfx.ModVolumeMultiplier,
            write: value => NewsanguoSfx.ModVolumeMultiplier = (float)value,
            save: () => NewsanguoSfx.SaveSfxConfig());
    }
}
