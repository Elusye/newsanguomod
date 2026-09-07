using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace newsanguo.Scripts.Settings;

/// <summary>
/// 把本 mod 卡牌/能力音效的开关与倍率注册为 RitsuLib 的 Mod 设置页（游戏内“设置 → Mod 设置”）。
///
/// 与控制台命令 newsanguo_sfx_volume 共用同一个持久化真值源（NewsanguoSfx.SfxEnabled /
/// NewsanguoSfx.ModVolumeMultiplier）：设置页控件的 Read/Write 直接读写这些属性
/// （Write 时属性 setter 会即时调音量并写盘），因此两处改动互相可见，不会出现各存一份的问题。
/// </summary>
public static class NewsanguoSfxVolumeSettings
{
    private const string ModId = Entry.ModId;

    private const string EnabledDataKey = "card_sfx_enabled";

    private const string VolumeDataKey = "card_sfx_volume_multiplier";

    public static void Register()
    {
        RitsuLibFramework.RegisterModSettings(ModId, page =>
        {
            page.WithSortOrder(0)
                .WithModDisplayName(ModSettingsText.Literal("新三国"))
                .WithTitle(ModSettingsText.Literal("新三国音效"))
                .WithDescription(ModSettingsText.Literal(
                    "调整新三国卡牌/能力音效的开关与音量，叠加在游戏“音效”音量之上。"))
                .AddSection("sfx_volume", section =>
                {
                    section.WithTitle(ModSettingsText.Literal("音效音量"))
                        .AddToggle(
                            id: "card_sfx_enabled",
                            label: ModSettingsText.Literal("启用卡牌音效"),
                            binding: CreateEnabledBinding(),
                            description: ModSettingsText.Literal(
                                "关闭后本 mod 的卡牌/能力/事件音效全部不播放（游戏其它音效不受影响）。"))
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
