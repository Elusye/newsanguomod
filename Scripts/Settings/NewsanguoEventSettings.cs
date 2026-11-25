using Godot;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.RunData;
namespace newsanguo.Scripts.Settings;
internal static class NewsanguoEventSettings
{
    public sealed class Rules { public bool Enabled { get; set; } = true; }
    private static RunSavedData<Rules> _rules = null!;
    private static string ConfigPath => Path.Combine(OS.GetUserDataDir(), "newsanguo_events_enabled.txt");
    internal static bool Preference { get; private set; } = true;
    internal static bool IsEnabled(IRunState runState) =>
        runState is RunState run && _rules is not null && _rules.TryGet(run, out var rules) ? rules.Enabled : true;
    internal static void SetPreference(bool enabled)
    {
        File.WriteAllText(ConfigPath, enabled ? "true" : "false");
        Preference = enabled;
    }
    internal static void Register()
    {
        Preference = !File.Exists(ConfigPath) || File.ReadAllText(ConfigPath).Trim() != "false";
        _rules = RitsuLibFramework.GetRunSavedDataStore(Entry.ModId).Register<Rules>("events_enabled");
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(evt =>
        {
            if (evt.IsHost) _rules.Lobby.Set(evt.Lobby, new Rules { Enabled = Preference });
        });
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(evt =>
        {
            if (!evt.IsMultiplayer) _rules.Set(evt.RunState, new Rules { Enabled = Preference });
        });
    }
}
