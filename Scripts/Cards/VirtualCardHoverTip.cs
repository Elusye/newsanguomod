using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace newsanguo.Scripts.Cards;

internal static class VirtualCardHoverTip
{
    public static IHoverTip Create() => new HoverTip(
        new LocString("static_hover_tips", "NEWSANGUO_VIRTUAL_CARD.title"),
        new LocString("static_hover_tips", "NEWSANGUO_VIRTUAL_CARD.description"));
}
