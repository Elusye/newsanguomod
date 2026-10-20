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
/// 新三国酒力机制与孙乾宇宙角色的活力联动：孙乾角色使用原版活力，
/// 其他角色继续使用新三国酒力。角色识别只走上面的公开 API 代理。
/// </summary>
internal static class SunqianUniverseVigorInterop
{
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

    public static IEnumerable<IHoverTip> ActiveResourceHoverTips(CardModel card)
    {
        Creature? owner = card.IsMutable ? card.Owner?.Creature : null;
        yield return UsesVigor(owner)
            ? HoverTipFactory.FromPower<VigorPower>()
            : HoverTipFactory.FromPower<DrunkenMightPower>();
    }

    public static IEnumerable<IHoverTip> AllyResourceHoverTips(CardModel card)
    {
        Creature? owner = card.IsMutable ? card.Owner?.Creature : null;
        bool ownerUsesVigor = UsesVigor(owner);

        yield return ownerUsesVigor
            ? HoverTipFactory.FromPower<VigorPower>()
            : HoverTipFactory.FromPower<DrunkenMightPower>();

        if (SunqianUniversePublicApiInterop.IsReady)
        {
            yield return ownerUsesVigor
                ? HoverTipFactory.FromPower<DrunkenMightPower>()
                : HoverTipFactory.FromPower<VigorPower>();
        }
    }

    public static void AddActiveResourceName(LocString description, CardModel card)
    {
        Creature? owner = card.IsMutable ? card.Owner?.Creature : null;
        description.Add("WinePowerName", GetActiveResourceName(owner));
    }

    public static void AddActiveResourceName(LocString description, PowerModel power)
    {
        Creature? owner = power.IsMutable ? power.Owner : null;
        description.Add("WinePowerName", GetActiveResourceName(owner));
    }

    private static string GetActiveResourceName(Creature? owner) =>
        (UsesVigor(owner)
            ? ModelDb.Power<VigorPower>().Title
            : ModelDb.Power<DrunkenMightPower>().Title).GetFormattedText();
}
