using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Settings;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class BalanceCardDescriptionPatch
{
    public static void Postfix(NCard __instance)
    {
        if (!__instance.IsNodeReady() || __instance.GetNodeOrNull<Control>("%Frame") is not { } frame)
            return;

        var border = frame.GetNodeOrNull<Panel>("NewsanguoBalanceBorder");
        bool visible = BalanceAdjustments.Enabled && __instance.Model is { } card && IsAffected(card);
        if (border is null && visible)
        {
            border = new Panel
            {
                Name = "NewsanguoBalanceBorder",
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            var style = new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                BorderColor = new Color("#59B8FF"),
                BorderWidthLeft = 3,
                BorderWidthTop = 3,
                BorderWidthRight = 3,
                BorderWidthBottom = 3,
                CornerRadiusTopLeft = 16,
                CornerRadiusTopRight = 16,
                CornerRadiusBottomLeft = 16,
                CornerRadiusBottomRight = 16
            };
            border.AddThemeStyleboxOverride("panel", style);
            frame.AddChild(border);
            border.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        if (border is not null)
            border.Visible = visible;
    }

    internal static bool IsAffected(CardModel card) => card is
        BetterThanYilingFlames or CreativeMode or DareYouToSlash or InvincibleWithinThreeHundredLi or LeaveItToFate or
        LetsGoDrinking or LoathToLeaveTheTable or PartyOn or SoImpressive or TigerWindCloudDragon;
}


[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
public static class BalanceCardHoverTipPatch
{
    public static void Postfix(CardModel __instance,
        ref IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> __result)
    {
        if (BalanceAdjustments.Enabled && BalanceCardDescriptionPatch.IsAffected(__instance))
        {
            __result = __result.Append(new MegaCrit.Sts2.Core.HoverTips.HoverTip(
                new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "NEWSANGUO_BALANCE_ADJUSTMENTS.title"),
                new MegaCrit.Sts2.Core.Localization.LocString("static_hover_tips", "NEWSANGUO_BALANCE_ADJUSTMENTS.description"))).ToList();
        }
    }
}
