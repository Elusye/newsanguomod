using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Api;

/// <summary>
/// 跨 Mod 联动的稳定公开 API。其它 Mod 应通过 RitsuLib <c>[ModInterop]</c> 调用，
/// 不要硬引用 newsanguo.dll。类名、命名空间与成员签名发布后视为契约。
/// </summary>
public static class NewsanguoPublicApi
{
    /// <summary>本程序集已加载时恒为 true，供 Interop 探测。</summary>
    public static bool IsReady => true;

    /// <summary>酒力能力的公开 Entry（<c>NEWSANGUO_POWER_DRUNKEN_MIGHT_POWER</c>）。</summary>
    public const string DrunkenMightPowerEntry = "NEWSANGUO_POWER_DRUNKEN_MIGHT_POWER";

    /// <summary>天意之力副资源的模组内 id（注册后完整 id 见 <see cref="GetHeavensForceId"/>）。</summary>
    public const string HeavensForceLocalId = HeavensForce.LocalId;

    /// <summary>天意侵蚀能力的公开 Entry（<c>NEWSANGUO_POWER_HEAVENS_DECAY_POWER</c>）。</summary>
    public const string HeavensDecayPowerEntry = "NEWSANGUO_POWER_HEAVENS_DECAY_POWER";

    public static bool IsNewsanguoCharacter(CharacterModel? character)
    {
        return character is NewsanguoCharacter;
    }

    public static Task ApplyDrunkenMight(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.Apply<DrunkenMightPower>(
            choiceContext,
            target,
            amount,
            applier,
            cardSource,
            silent);
    }

    public static IHoverTip CreateDrunkenMightHoverTip()
    {
        return HoverTipFactory.FromPower<DrunkenMightPower>();
    }

    /// <summary>
    /// 供卡面 <c>CanonicalVars</c> 使用。必须是 <c>PowerVar&lt;DrunkenMightPower&gt;</c>，
    /// 卡面数字才会计入「换大盏」「不胜酒力」等 Given 侧修正。
    /// </summary>
    public static DynamicVar CreateDrunkenMightVar(decimal amount)
    {
        return new PowerVar<DrunkenMightPower>(amount);
    }

    /// <summary>
    /// 天意之力注册后的完整资源 id。未完成 <see cref="HeavensForce.Register"/> 时为空。
    /// 增减数值必须走 <see cref="AddHeavensForce"/> 等封装，不要对隐藏载体
    /// <c>HeavensForcePower</c> 调用 <c>PowerCmd.Apply</c>。
    /// </summary>
    public static string GetHeavensForceId()
    {
        return HeavensForce.Id;
    }

    public static int GetHeavensForce(Player? player)
    {
        return HeavensForce.Get(player);
    }

    public static int GetHeavensForceLostThisCombat(Player? player)
    {
        return HeavensForce.LostThisCombat(player);
    }

    public static int GetHeavensForceGainedThisCombat(Player? player)
    {
        return HeavensForce.GainedThisCombat(player);
    }

    /// <summary>
    /// 按增量变动天意之力（正为获得、负为失去）。卡牌打出时把 <paramref name="source"/> 设为该牌。
    /// </summary>
    public static Task AddHeavensForce(
        PlayerChoiceContext choiceContext,
        Player? player,
        int delta,
        AbstractModel? source = null)
    {
        return HeavensForce.Add(choiceContext, player, delta, source);
    }

    public static Task SetHeavensForce(
        PlayerChoiceContext choiceContext,
        Player? player,
        int amount,
        AbstractModel? source = null)
    {
        return HeavensForce.Set(choiceContext, player, amount, source);
    }

    public static IHoverTip CreateHeavensForceHoverTip()
    {
        return HeavensForce.HoverTip();
    }

    /// <summary>
    /// 供卡面 <c>CanonicalVars</c> 使用。变量名是 <c>HeavensForcePower</c>，
    /// 文案用 <c>{HeavensForcePower:diff()}</c> 或消耗用 <c>inverseDiff()</c>。
    /// </summary>
    public static DynamicVar CreateHeavensForceVar(decimal amount)
    {
        return new HeavensForceVar(amount);
    }

    public static Task ApplyHeavensDecay(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.Apply<HeavensDecayPower>(
            choiceContext,
            target,
            amount,
            applier,
            cardSource,
            silent);
    }

    public static IHoverTip CreateHeavensDecayHoverTip()
    {
        return HoverTipFactory.FromPower<HeavensDecayPower>();
    }
}
