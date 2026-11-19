using Godot;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace newsanguo.Scripts.Settings;

internal static class BalanceAdjustments
{
    public sealed class Rules
    {
        public bool Enabled { get; set; }
        public int ChallengeLevel { get; set; }
    }

    internal const string DataKey = "balance_adjustments";

    // 暂时隐藏调整与挑战入口；原版向调整统一关闭。
    internal static bool ShowAdjustmentOptions => false;

    [ThreadStatic]
    internal static bool? RestoringRules;

    private static RunSavedData<Rules> _rules = null!;
    private static string ConfigPath => Path.Combine(OS.GetUserDataDir(), "newsanguo_balance_adjustments.txt");
    internal static bool Preference { get; private set; }

    internal static int SelectedChallengeLevel { get; set; }

    private static RunState? ActiveRun => RunManager.Instance?.DebugOnlyGetState();

    internal static int ChallengeLevel => ActiveRun is { } run &&
        _rules is not null && _rules.TryGet(run, out var rules) ? rules.ChallengeLevel : 0;

    // 不读取本地偏好、挑战等级或读档规则，确保所有场景始终关闭。
    internal static bool Enabled => false;

    private static Rules CreateRules() => new()
    {
        Enabled = false,
        ChallengeLevel = SelectedChallengeLevel
    };

    internal static void Register()
    {
        Preference = File.Exists(ConfigPath) && File.ReadAllText(ConfigPath).Trim() == "true";
        _rules = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).Register<Rules>(DataKey);
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(evt =>
        {
            if (evt.IsHost)
            {
                _rules.Lobby.Set(evt.Lobby, CreateRules());
            }
        });
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(evt =>
        {
            if (!evt.IsMultiplayer)
            {
                _rules.Set(evt.RunState, CreateRules());
            }
        });
    }

    internal static void SetPreference(bool enabled)
    {
        File.WriteAllText(ConfigPath, enabled ? "true" : "false");
        Preference = enabled;
    }
}
