using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

/// <summary>记录并复制本回合最近打出的其他牌；打出同名牌不覆盖记录。</summary>
[RegisterCard(typeof(NeutralCardPool))]
public class SoImpressive : NeutralCardTemplate
{
    private CardModel? _copiedTarget;
    private CardModel? _recordedCard;
    private int _recordedTurn = -1;
    private string? _playedTitle;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    public SoImpressive() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    private CardModel? DisplaySource => IsDupe && _copiedTarget is not null
        ? _copiedTarget : _recordedCard ?? ModelDb.Card<SunBen>();

    private static string SourceName(CardModel? source) => source?.Title ?? string.Empty;
    private string DynamicTitle(string name)
    {
        LocString title = new("cards", "NEWSANGUO_CARD_SO_IMPRESSIVE.combatTitle");
        title.Add("PreviousCard", name);
        return title.GetFormattedText() + (IsUpgraded ? "+" : "");
    }
    public override string Title
    {
        get
        {
            if (!IsMutable || !IsInCombat || CombatState is null) return base.Title;
            if (Pile?.Type == PileType.Play && _playedTitle is not null) return _playedTitle;
            return DynamicTitle(SourceName(DisplaySource));
        }
    }
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("PreviousCard", SourceName(Pile?.Type == PileType.Play
            ? _copiedTarget ?? DisplaySource : DisplaySource));
    }
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (!IsMutable || !IsInCombat || CombatState is null)
                return System.Array.Empty<IHoverTip>();
            CardModel? source = DisplaySource;
            if (source is null) return System.Array.Empty<IHoverTip>();
            return [HoverTipFactory.FromCard(source)];
        }
    }
    private void InitializeRecord()
    {
        if (!IsMutable || CombatState is null) return;
        int turn = Owner.PlayerCombatState!.TurnNumber;
        if (_recordedCard is not null && _recordedTurn == turn) return;
        _recordedTurn = turn;
        _copiedTarget = null;
        _playedTitle = null;
        // 按实际开始出牌的顺序恢复记录，也包括正在结算并生成此牌的来源牌。
        _recordedCard = CombatManager.Instance.History.CardPlaysStarted
            .LastOrDefault(entry => entry.HappenedThisTurn(CombatState) &&
                entry.CardPlay.Player == Owner && entry.CardPlay.Card is not SoImpressive)
            ?.CardPlay.Card ?? CombatState.CreateCard<SunBen>(Owner);
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner) { InitializeRecord(); RefreshHandTitle(); }
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this) { InitializeRecord(); RefreshHandTitle(); }
        return Task.CompletedTask;
    }
    private void RefreshHandTitle()
    {
        if (Pile?.Type == PileType.Hand)
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this) { InitializeRecord(); RefreshHandTitle(); }
        return Task.CompletedTask;
    }
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner) return Task.CompletedTask;
        InitializeRecord();
        if (cardPlay.Card is not SoImpressive)
        {
            // 在出牌开始时覆盖，嵌套自动出牌也按实际出牌顺序记录。
            _recordedCard = cardPlay.Card;
            RefreshHandTitle();
        }
        else if (cardPlay.Card == this && (!IsDupe || _copiedTarget is null))
        {
            // 冻结这次复制目标；自身及其他同名牌均不覆盖记录。
            _copiedTarget = _recordedCard;
            _playedTitle = DynamicTitle(SourceName(_recordedCard));
        }
        return Task.CompletedTask;
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_copiedTarget is not null)
        {
            // 与History Course相同：保留升级、附魔状态，自动选择合法目标，不支付复制品能量。
            using (var playback = NewsanguoSfx.CapturePlayback())
            {
                await CardCmd.AutoPlay(choiceContext, _copiedTarget.CreateDupe(Owner), null);
                await playback.WaitAsync(leadSeconds: 0.5);
            }
        }
        NewsanguoSfx.Play("event:/newsanguo/sfx/so_impressive");
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        RefreshHandTitle();
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
