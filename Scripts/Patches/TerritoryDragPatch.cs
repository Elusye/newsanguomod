using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;
namespace newsanguo.Scripts.Patches;

/// <summary>地域牌在手牌区域松开时换位，进入出牌区域后仍走原版指定目标流程。</summary>
[HarmonyPatch(typeof(NMouseCardPlay), "_Input")]
public static class TerritoryDragPatch
{
    public static bool Prefix(NMouseCardPlay __instance, InputEvent inputEvent)
    {
        if (__instance.Holder.CardNode?.Model is not TerritoryCardTemplate card ||
            inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
            return true;
        Vector2 mouse = __instance.GetViewport().GetMousePosition();
        float threshold = (float)AccessTools.Property(typeof(NMouseCardPlay), "PlayZoneThreshold").GetValue(__instance)!;
        if (mouse.Y < threshold) return true;
        var hand = NPlayerHand.Instance;
        if (hand is null) return true;
        int index = hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>()
            .Count(h => h.CardNode?.Model != card && h.GlobalPosition.X < mouse.X);
        // 先取消原版出牌拖拽，让本体回到手牌容器，再提交同步换位操作。
        __instance.CancelPlayCard();
        if (TerritoryHandOrder.CanReposition(card))
            TerritoryHandOrder.Request(card, index);
        __instance.GetViewport().SetInputAsHandled();
        return false;
    }
}

// 地域牌无法打出，需绕过原版手牌金光的 CanPlay 限制；选牌模式仍使用原版判定。
[HarmonyPatch(typeof(NHandCardHolder), "ShouldGlowGold", MethodType.Getter)]
public static class TerritoryRepositionGlowPatch
{
    public static void Postfix(NHandCardHolder __instance, ref bool __result)
    {
        if (!__result && NPlayerHand.Instance is { SelectModeGoldGlowOverride: null } &&
            __instance.CardNode?.Model is InvincibleWithinThreeHundredLi card && card.ShouldGlowGold)
            __result = true;
    }
}
