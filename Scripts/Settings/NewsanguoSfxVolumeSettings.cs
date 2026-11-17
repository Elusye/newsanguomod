using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace newsanguo.Scripts.Settings;

/// <summary>
/// 把本 mod 的设置项注册为 RitsuLib 的 Mod 设置页（游戏内“设置 → Mod 设置”）。
/// 音效开关和音量倍率设置。
///
/// 与控制台命令 newsanguo_sfx_volume 共用同一个持久化真值源（NewsanguoSfx.SfxEnabled /
/// NewsanguoSfx.ModVolumeMultiplier）：设置页控件的 Read/Write 直接读写这些属性
/// （Write 时属性 setter 会即时调音量并写盘），因此两处改动互相可见，不会出现各存一份的问题。
/// “打击/防御/士兵/灵魂锁链”出牌音效开关（NewsanguoSfx.BasicCardSfxEnabled）同样持久化到该文件。
/// </summary>
public static class NewsanguoSfxVolumeSettings
{
    private const string ModId = Entry.ModId;
    private const string PageId = "newsanguo";
    private const string SoundSectionId = "sfx_volume";

    internal static ModSettingsOpenResult OpenVolumeSettings() =>
        ModSettingsNavigator.RequestOpenByIds(ModId, PageId, SoundSectionId, VolumeDataKey);

    private const string EnabledDataKey = "card_sfx_enabled";

    private const string VolumeDataKey = "card_sfx_volume_multiplier";

    private const string BasicCardDataKey = "basic_card_sfx_enabled";

    public static void Register()
    {
        BalanceAdjustments.Register();
        RitsuLibFramework.RegisterModSettings(ModId, page =>
        {
            page.WithSortOrder(0)
                .WithModDisplayName(ModSettingsText.Literal("新三国"))
                .WithTitle(ModSettingsText.Literal("新三国设置"))
                .WithDescription(ModSettingsText.Literal(
                    "新三国 mod 的通用设置：卡牌、能力及事件音效。"))
                .AddSection("balance", section => section
                    .WithTitle(ModSettingsText.Literal("玩法"))
                    .AddToggle(
                        id: "balance_adjustments",
                        label: ModSettingsText.Literal("平衡性调整"),
                        binding: ModSettingsBindings.Callback<bool>(
                            ModId, "balance_adjustments", () => BalanceAdjustments.Preference,
                            BalanceAdjustments.SetPreference, () => { }),
                        description: ModSettingsText.Literal(
                            "新开跑局生效。联机以房主设置为准，挑战 1 或更高也会启用。")))
                .AddSection("balance_details", section => section
                    .WithTitle(ModSettingsText.Literal("平衡性调整清单"))
                    .Collapsible(true)
                    .AddParagraph("balance_values", ModSettingsText.Literal(
                        """
                        扎聋我自己的耳朵！：罕见→普通，帝王之征 15/20→8/11 层。
                        四轮车：格挡 4/5→3/4。
                        无能忧虑：格挡 16/20→10/13。
                        人肉打印机：复制 5→3 张牌。
                        医术高明：基础伤害 15→10。
                        全死了我都值！：费用 1→2。
                        童谣：费用 1→2。
                        从来没有这些！：升级只将格挡从 8 提升到 11，每张消耗的牌仍提供 1 点天意之力。
                        占卜：抽 1/2 张牌，升级前后均获得 2 点天意之力。
                        制式大营：精甲 6/8→3/4。
                        星夜：格挡 8/11→6/8。
                        一万个透明窟窿！：每段 3 点伤害，攻击 3X/4X 次。
                        前途灿烂：费用 2/2→3/2，每回合打出 1 张随机能力牌。
                        """), ModSettingsText.Literal("数值与费用（基础／升级）"))
                    .AddParagraph("balance_effects", ModSettingsText.Literal(
                        """
                        创造模式：战后选牌排除先古稀有度。
                        方圆三百里无敌：相邻牌的伤害翻倍，取代重放。
                        风从虎，云从龙：龙牌和笑面虎获得重放 1，取代抽牌和生成牌。
                        笑面虎：移除龙牌身份。
                        好厉害呀！：成功打出能力牌后消耗自身。
                        走，喝酒去！：基础版和升级版都有消耗。
                        接着奏乐接着舞：增加消耗。
                        听天由命：普通→罕见，获得 1 层易伤和 1 层虚弱，取代脆弱。
                        """), ModSettingsText.Literal("机制与词条")))
                .AddSection(SoundSectionId, section =>
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
                            id: VolumeDataKey,
                            label: ModSettingsText.Literal("卡牌音效倍率"),
                            binding: CreateVolumeBinding(),
                            minValue: 0,
                            maxValue: 4,
                            step: 0.05,
                            valueFormatter: value => value.ToString("0.##") + "×",
                            description: ModSettingsText.Literal(
                                "1× = 默认音量；0 = 静音；最高 4×。也可用控制台命令 newsanguo_sfx_volume 调整。"));
                });
        }, pageId: PageId);
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
