using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
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

    // ------------------------------------------------------------------
    // 下面这批入口是为「不想硬引用 newsanguo.dll 的下游 Mod」补的：
    // 它们原本要自己摸 CreateCard/AddGeneratedCardsToCombat 或直接引用我们的 Power 类型，
    // 现在统一走这里。签名发布后视为契约。
    // ------------------------------------------------------------------

    /// <summary>当前酒力层数（没有该能力时为 0）。用于按酒力计算数值，例如「每点酒力额外 X 点格挡」。</summary>
    public static int GetDrunkenMight(Player? player)
    {
        return player?.Creature?.GetPower<DrunkenMightPower>()?.Amount ?? 0;
    }

    /// <summary>
    /// 生成 <paramref name="count"/> 张指定 Token 卡并加入手牌；等价于
    /// <c>AddTokenToHand(player, kind, count, PileType.Hand)</c>。仅为兼容已编译的调用方而保留。
    /// </summary>
    public static Task<int> AddTokenToHand(Player player, NewsanguoTokenKind kind, int count = 1)
    {
        return AddTokenToHand(player, kind, count, PileType.Hand);
    }

    /// <summary>
    /// 生成 <paramref name="count"/> 张指定 Token 卡并放入 <paramref name="destination"/>。
    /// <para>只接受 <see cref="PileType.Hand"/> / <see cref="PileType.Discard"/> / <see cref="PileType.Draw"/>；
    /// <see cref="PileType.Play"/> 与 <see cref="PileType.Deck"/> 抛 <see cref="ArgumentOutOfRangeException"/>
    /// （战斗中往这两个堆塞生成卡几乎不会是调用方的本意，静默接受会变成难查的 bug）。</para>
    /// <para>去向语义：<c>Hand</c> 手牌已满时由引擎自动转入弃牌堆（<b>不是</b>错误路径，调用方无法与直接指定
    /// <c>Discard</c> 区分）；<c>Draw</c> 插入抽牌堆<b>底部</b>（引擎 <c>AddGeneratedCardsToCombat</c> 默认
    /// <c>CardPilePosition.Bottom</c>）且<b>不洗牌</b>，所以"加入抽牌堆"不等于"下回合一定抽到"；
    /// <c>Discard</c> 放入弃牌堆，无排序问题。</para>
    /// <b>不</b>施加任何额外效果，<b>不</b>触发额外音效；战斗已结束或正在结束时返回 0 且不生成任何卡。
    /// </summary>
    /// <returns>实际生成的张数。</returns>
    public static async Task<int> AddTokenToHand(Player player, NewsanguoTokenKind kind, int count, PileType destination)
    {
        if (player is null || count <= 0 || CombatManager.Instance.IsOverOrEnding)
        {
            return 0;
        }
        if (destination is not (PileType.Hand or PileType.Discard or PileType.Draw))
        {
            throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination,
                "Token 只能生成到 PileType.Hand / Discard / Draw。");
        }
        if (player.Creature?.CombatState is not { } combatState)
        {
            return 0;
        }

        int created = 0;
        for (int i = 0; i < count; i++)
        {
            CardModel? token = kind switch
            {
                NewsanguoTokenKind.MilitaryCudgel => await MilitaryCudgel.CreateInPile(player, combatState, destination),
                NewsanguoTokenKind.Soldier => await AddGeneratedToPile(combatState.CreateCard<Soldier>(player), player, destination),
                NewsanguoTokenKind.Lightweight => await AddGeneratedToPile(combatState.CreateCard<Lightweight>(player), player, destination),
                _ => null,
            };
            if (token is not null)
            {
                created++;
            }
        }

        return created;
    }

    /// <summary>
    /// 把 <paramref name="sourceCard"/> 的复制品（含升级/附魔等当前状态）加入 <paramref name="player"/> 的手牌，
    /// 与「乌角鲨」「笑面虎」的抽到即复制同款实现。手牌溢出由引擎自动转入弃牌堆。
    /// </summary>
    /// <returns>最后一张复制品；一张都没生成时为 null。</returns>
    public static async Task<CardModel?> CopyCardToHand(Player player, CardModel sourceCard, int count = 1)
    {
        if (player is null || sourceCard is null || count <= 0)
        {
            return null;
        }

        CardPile hand = PileType.Hand.GetPile(player);
        CardModel? last = null;
        for (int i = 0; i < count; i++)
        {
            last = sourceCard.CreateClone();
            await CardPileCmd.Add(last, hand);
        }

        return last;
    }

    /// <summary>施加「国贼」：目标身上的易伤无法被减少（含回合结束的自然衰减）。「国贼董卓嘛！」同款。</summary>
    public static Task ApplyTraitorTyranny(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.Apply<TraitorTyrannyPower>(choiceContext, target, amount, applier, cardSource, silent);
    }

    public static IHoverTip CreateTraitorTyrannyHoverTip()
    {
        return HoverTipFactory.FromPower<TraitorTyrannyPower>();
    }

    /// <summary>施加「缠身」：目标本回合不能打出攻击牌。</summary>
    public static Task ApplyEntangled(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.Apply<EntangledPower>(choiceContext, target, amount, applier, cardSource, silent);
    }

    public static IHoverTip CreateEntangledHoverTip()
    {
        return HoverTipFactory.FromPower<EntangledPower>();
    }

    /// <summary>施加「帝王之征」。</summary>
    public static Task ApplyDragonOmen(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.Apply<DragonOmenPower>(choiceContext, target, amount, applier, cardSource, silent);
    }

    public static IHoverTip CreateDragonOmenHoverTip()
    {
        return HoverTipFactory.FromPower<DragonOmenPower>();
    }

    /// <summary>施加「飞行」。</summary>
    public static Task ApplyFlight(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return PowerCmd.Apply<FlightPower>(choiceContext, target, amount, applier, cardSource, silent);
    }

    public static IHoverTip CreateFlightHoverTip()
    {
        return HoverTipFactory.FromPower<FlightPower>();
    }

    /// <summary>预见 N（与「窥探天意」「二周目玩家」同一实现）。<b>不</b>抽牌、<b>不</b>改动手牌。</summary>
    public static Task Scry(PlayerChoiceContext choiceContext, Player player, int amount)
    {
        return ScryCmd.Scry(choiceContext, player, amount);
    }

    /// <summary>
    /// 击晕目标（转调原版 <c>CreatureCmd.Stun</c>，与「心灵控制术」同款）。
    /// 只有"击晕这一名生物"这一个动作：<b>不</b>施加层数、<b>不</b>消耗任何资源、<b>不</b>有额外音效。
    /// "击晕 N 名敌人"请由调用方自己选好 N 个目标后逐个调用。
    /// </summary>
    public static Task Stun(Creature target)
    {
        return CreatureCmd.Stun(target);
    }

    /// <summary>
    /// 消耗 <paramref name="player"/> 手牌中的<b>所有非攻击牌</b>，并按实际消耗张数为他增加
    /// <paramref name="perCard"/> 点/张的天意之力（与「从来就没有这些！」同一实现：
    /// 先一次性把全部非攻击牌消耗掉，再按张数结算一次天意之力）。
    /// <b>不</b>获得格挡、<b>不</b>影响攻击牌；手牌里没有非攻击牌时返回 0 且不结算天意之力。
    /// </summary>
    /// <returns>实际消耗的手牌张数。</returns>
    public static async Task<int> ExhaustNonAttackCardsForHeavens(
        PlayerChoiceContext choiceContext,
        Player player,
        decimal perCard,
        CardModel? cardSource)
    {
        if (player is null)
        {
            return 0;
        }

        List<CardModel> nonAttackCards = PileType.Hand.GetPile(player).Cards
            .Where(card => card.Type != CardType.Attack)
            .ToList();
        if (nonAttackCards.Count == 0)
        {
            return 0;
        }

        foreach (CardModel card in nonAttackCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        int total = (int)(perCard * nonAttackCards.Count);
        if (total != 0)
        {
            await HeavensForce.Add(choiceContext, player, total, cardSource);
        }

        return nonAttackCards.Count;
    }

    /// <summary>
    /// 让玩家从手牌中选择任意张（可 0 张）并逐张变化为指定 Token
    /// （与「人体炼成术」同一实现；<paramref name="upgradeTokens"/> 为 true 时变化出的 Token 带升级）。
    /// <b>不</b>产生其它效果、<b>不</b>退还费用、<b>不</b>额外消耗牌；手牌为空时直接返回 0（不弹选择界面）。
    /// </summary>
    /// <returns>实际变化的张数。</returns>
    public static async Task<int> TransformSelectedHandCardsToToken(
        PlayerChoiceContext choiceContext,
        Player player,
        NewsanguoTokenKind kind,
        bool upgradeTokens = false)
    {
        if (player?.Creature?.CombatState is not { } combatState)
        {
            return 0;
        }
        CardPile hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count == 0)
        {
            return 0;
        }

        List<CardModel> selected = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: player,
            prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_ANY"), 0, hand.Cards.Count),
            filter: null,
            source: null)).ToList();

        int changed = 0;
        foreach (CardModel original in selected)
        {
            CardModel? token = CreateToken(kind, combatState, player);
            if (token is null)
            {
                continue;
            }
            if (upgradeTokens)
            {
                CardCmd.Upgrade(token);
            }
            await CardCmd.Transform(original, token);
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// 把本机听到的音量降到 25%（与「扎聋我自己的耳朵！」同一副作用），战斗结束 / 读档 / 回主菜单时自动恢复。
    /// 只在调用方本机生效（多人下请自行用 <c>LocalContext.IsMe</c> 判定）；幂等，重复调用安全。
    /// 注意：当前实现固定 25%，没有任意百分比的入口（任意比例需要改 mod 的音频路径，尚未提供）。
    /// </summary>
    public static void ReduceCombatHearingVolume()
    {
        HearingVolumeController.ReduceToQuarterVolume();
    }

    /// <summary>立即把本机音量恢复到用户设置值（幂等）。</summary>
    public static void RestoreHearingVolume()
    {
        HearingVolumeController.RestoreFullVolume();
    }

    /// <summary>当前是否处于"听觉下降"状态。</summary>
    public static bool IsCombatHearingVolumeReduced => HearingVolumeController.IsReduced;

    private static CardModel? CreateToken(NewsanguoTokenKind kind, ICombatState combatState, Player player)
    {
        return kind switch
        {
            NewsanguoTokenKind.MilitaryCudgel => combatState.CreateCard<MilitaryCudgel>(player),
            NewsanguoTokenKind.Soldier => combatState.CreateCard<Soldier>(player),
            NewsanguoTokenKind.Lightweight => combatState.CreateCard<Lightweight>(player),
            _ => null,
        };
    }

    private static async Task<CardModel?> AddGeneratedToPile(CardModel card, Player player, PileType destination)
    {
        await CardPileCmd.AddGeneratedCardsToCombat([card], destination, player);
        return card;
    }
}
