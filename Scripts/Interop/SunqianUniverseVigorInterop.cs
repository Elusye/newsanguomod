using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using newsanguo.Scripts.Powers;
using STS2RitsuLib.Interop;

namespace newsanguo.Scripts.Interop;

/// <summary>
/// 孙乾宇宙公开 API 的可选依赖代理。未安装目标模组时保持默认返回值，
/// 不得直接引用 sunqian-universe.dll。
/// </summary>
[ModInterop("sunqian-universe", "Squ.Api.SunqianUniversePublicApi")]
public static class SunqianUniversePublicApiInterop
{
    public static bool IsReady => false;

    public static bool IsSupportingActorCharacter(CharacterModel? character) => false;
}

/// <summary>
/// 新三国酒力触发条件与孙乾宇宙角色的活力联动：孙乾角色监听原版活力，
/// 其他角色继续监听新三国酒力。《咱家不怕酸》的补偿资源也随角色切换，
/// 其余效果只切换触发条件，不改触发后的原效果。
/// 角色识别只走上面的公开 API 代理。
/// </summary>
internal static class SunqianUniverseVigorInterop
{
    private const string HoverTitleKey = "NEWSANGUO_SUNQIAN_VIGOR_INTEROP.hoverTitle";
    private const string GainHoverKey = "NEWSANGUO_SUNQIAN_VIGOR_INTEROP.gainHover";
    private const string LoseHoverKey = "NEWSANGUO_SUNQIAN_VIGOR_INTEROP.loseHover";

    private static JointModPresentationMode PresentationMode =>
        JointModPresentation.Resolve(
            SunqianUniversePublicApiInterop.IsReady,
            SunqianUniversePublicApiInterop.IsSupportingActorCharacter);

    public static bool UsesVigor(Creature? creature) =>
        SunqianUniversePublicApiInterop.IsReady
        && SunqianUniversePublicApiInterop.IsSupportingActorCharacter(creature?.Player?.Character);

    public static bool IsActiveResourcePower(PowerModel power, Creature? owner) =>
        UsesVigor(owner)
            ? power is VigorPower
            : power is DrunkenMightPower;

    public static Task ApplyActiveResource(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false) =>
        UsesVigor(target)
            ? PowerCmd.Apply<VigorPower>(choiceContext, target, amount, applier, cardSource, silent)
            : PowerCmd.Apply<DrunkenMightPower>(choiceContext, target, amount, applier, cardSource, silent);

    public static IEnumerable<IHoverTip> ActiveResourceHoverTips(CardModel _)
    {
        yield return PresentationMode == JointModPresentationMode.InlineKeywords
            ? HoverTipFactory.FromPower<VigorPower>()
            : HoverTipFactory.FromPower<DrunkenMightPower>();
    }

    public static IEnumerable<IHoverTip> TriggerAndDrunkenMightHoverTips(CardModel _)
    {
        bool inlineKeywords = PresentationMode == JointModPresentationMode.InlineKeywords;
        yield return inlineKeywords
            ? HoverTipFactory.FromPower<VigorPower>()
            : HoverTipFactory.FromPower<DrunkenMightPower>();

        if (inlineKeywords)
        {
            yield return HoverTipFactory.FromPower<DrunkenMightPower>();
        }
    }

    public static IEnumerable<IHoverTip> InteractionHoverTips(bool gainsResource)
    {
        if (PresentationMode != JointModPresentationMode.HoverOnly)
        {
            yield break;
        }

        yield return new HoverTip(
            new LocString("static_hover_tips", HoverTitleKey),
            new LocString("static_hover_tips", gainsResource ? GainHoverKey : LoseHoverKey));
    }

    public static void AddActiveResourceName(LocString description, CardModel _)
    {
        description.Add("WinePowerName", GetActiveResourceInlineText());
    }

    public static void AddActiveResourceName(LocString description, PowerModel _)
    {
        description.Add("WinePowerName", GetActiveResourceInlineText());
    }

    private static string GetActiveResourceInlineText()
    {
        bool inlineKeywords = PresentationMode == JointModPresentationMode.InlineKeywords;
        string name = (inlineKeywords
                ? ModelDb.Power<VigorPower>().Title
                : ModelDb.Power<DrunkenMightPower>().Title)
            .GetFormattedText();

        return inlineKeywords
            ? $"[purple]{name}[/purple]"
            : $"[gold]{name}[/gold]";
    }
}
