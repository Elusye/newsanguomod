using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Interop;
using newsanguo.Scripts.Powers;
using newsanguo.Scripts.Settings;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), "AfterInitialized")]
public static class ChallengeSelectionPatch
{
    public static void Postfix(NCharacterSelectScreen __instance)
    {
        var panel = __instance.GetNode<Control>("%AscensionPanel");
        if (__instance.HasNode("NewsanguoSelectOptions"))
            return;

        bool chinese = LocManager.Instance.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        var selector = new OptionButton
        {
            Name = "NewsanguoChallenge", ZIndex = 1,
            Visible = BalanceAdjustments.ShowAdjustmentOptions
        };
        string[] labels = chinese
            ? ["挑战模式：关闭", "挑战 1：原版向调整", "挑战 2：酒力获得量降低", "挑战 3：开战失去天意之力"]
            : ["Challenge Mode: Off", "Challenge 1: Vanilla-Oriented Adjustments", "Challenge 2: Reduced Wine", "Challenge 3: Lose Heaven's Force"];
        foreach (string label in labels)
            selector.AddItem(label);

        selector.TooltipText = chinese
            ? "挑战等级累计生效。\n1：启用原版向调整。\n2：卡牌给予的酒力降低 25%，向下取整。\n3：每场战斗开始时失去 5 点天意之力。"
            : "Challenges are cumulative.\n1: Enable vanilla-oriented adjustments.\n2: Cards grant 25% less Wine, rounded down.\n3: Lose 5 Heaven's Force at the start of each combat.";

        if (__instance.Lobby.NetService.Type == NetGameType.Client)
        {
            selector.Clear();
            selector.AddItem(chinese ? "挑战模式：由房主选择" : "Challenge Mode: Set by Host");
            selector.Disabled = true;
        }
        else
        {
            selector.Selected = BalanceAdjustments.SelectedChallengeLevel;
            selector.ItemSelected += index => BalanceAdjustments.SelectedChallengeLevel = (int)index;
        }

        var settings = new Button
        {
            Name = "NewsanguoSettings",
            Text = chinese ? "新三国设置" : "Newsanguo Settings",
            TooltipText = chinese ? "打开新三国的音效音量设置" : "Open Newsanguo's sound volume settings"
        };
        settings.Pressed += () =>
        {
            var result = NewsanguoSfxVolumeSettings.OpenVolumeSettings();
            if (!result.Success)
                GD.PushWarning($"[newsanguo] Could not open sound settings: {result.Message}");
        };

        var ascensionDescription = panel.GetNode<RichTextLabel>("HBoxContainer/AscensionDescription/Description");
        var font = ascensionDescription.GetThemeFont("normal_font");
        const int fontSize = 22;
        selector.AddThemeFontOverride("font", font);
        selector.AddThemeFontSizeOverride("font_size", fontSize);
        var popup = selector.GetPopup();
        popup.AddThemeFontOverride("font", font);
        popup.AddThemeFontSizeOverride("font_size", fontSize);
        settings.AddThemeFontOverride("font", font);
        settings.AddThemeFontSizeOverride("font_size", fontSize);

        var options = new HBoxContainer { Name = "NewsanguoSelectOptions", ZIndex = 1 };
        options.AddThemeConstantOverride("separation", 10);
        options.AddChild(selector);
        options.AddChild(settings);
        __instance.AddChild(options);
        void Reposition()
        {
            if (!GodotObject.IsInstanceValid(options))
                return;

            var rect = panel.GetGlobalRect();
            options.GlobalPosition = new Vector2(
                rect.Position.X + (rect.Size.X - options.Size.X) / 2,
                rect.Position.Y - options.Size.Y - 8);
        }

        panel.ItemRectChanged += Reposition;
        options.Resized += Reposition;
        Callable.From(Reposition).CallDeferred();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountGiven))]
public static class ChallengeWinePatch
{
    public static void Postfix(PowerModel power, Creature giver, decimal amount, Creature? target,
        CardModel? cardSource, ref decimal __result)
    {
        if (BalanceAdjustments.ChallengeLevel < 2 || cardSource is null || amount <= 0 || __result <= 0)
            return;

        if (power is DrunkenMightPower ||
            cardSource is NewsanguoCardTemplate &&
            SunqianUniverseVigorInterop.IsActiveResourcePower(power, target ?? giver))
        {
            __result = Math.Floor(__result * 0.75m);
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
public static class ChallengeCombatStartPatch
{
    public static void Postfix(IRunState runState, ref Task __result)
    {
        if (BalanceAdjustments.ChallengeLevel >= 3)
            __result = LoseHeavensForce(__result, runState);
    }

    private static async Task LoseHeavensForce(Task original, IRunState runState)
    {
        await original;
        var context = new ThrowingPlayerChoiceContext();
        foreach (var player in runState.Players)
            await HeavensForce.Lose(context, player, 5);
    }
}
