using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Models.CardPools;
namespace newsanguo.Scripts.Cards;

/// <summary>同步随机锁定弃牌；预览不消耗随机数，结算使用同一张原牌并将其消耗。</summary>
[RegisterCard(typeof(TokenCardPool))]
public class NoReturn : NewsanguoCardTemplate
{
    private CardModel? _lockedTarget;
    private CardModel? _resolvingCard;
    private string? _playedTargetName;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/NoReturn.png");
    public override bool CanBeGeneratedInCombat => false;
    public NoReturn() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self, showInCardLibrary: false) { }

    private bool IsEligible(CardModel card) => card != this && card.Owner == Owner &&
        card.Pile?.Type == PileType.Discard && !card.Keywords.Contains(CardKeyword.Unplayable) &&
        !TerritoryCardTemplate.HasTerritoryKeyword(card);
    private bool HasTarget => IsMutable && IsInCombat && CombatState is not null &&
        (Pile?.Type == PileType.Play && _playedTargetName is not null ||
         _lockedTarget is not null && IsEligible(_lockedTarget));
    private string TargetName => Pile?.Type == PileType.Play && _playedTargetName is not null
        ? _playedTargetName : _lockedTarget?.Title ?? string.Empty;
    public override string Title
    {
        get
        {
            if (!HasTarget || Pile?.Type is not (PileType.Hand or PileType.Play)) return base.Title;
            var title = new LocString("cards", "NEWSANGUO_CARD_NO_RETURN.combatTitle");
            title.Add("TargetCard", TargetName);
            return title.GetFormattedText() + (IsUpgraded ? "+" : "");
        }
    }
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("TargetCard", HasTarget ? TargetName :
            new LocString("cards", "NEWSANGUO_CARD_NO_RETURN.anyCard").GetFormattedText());
    }
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    private void RefreshTarget()
    {
        if (!IsMutable || !IsInCombat || CombatState is null ||
            Pile?.Type is not (PileType.Hand or PileType.Play) || CombatManager.Instance.IsOverOrEnding)
            return;
        if (_lockedTarget is not null && IsEligible(_lockedTarget)) return;
        var candidates = PileType.Discard.GetPile(Owner).Cards.Where(IsEligible).ToList();
        _lockedTarget = candidates.Count == 0 ? null : Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (Pile.Type == PileType.Hand)
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (Pile?.Type == PileType.Hand && card.Owner == Owner) RefreshTarget();
        return Task.CompletedTask;
    }
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this) RefreshTarget();
        return Task.CompletedTask;
    }
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card == this)
        {
            RefreshTarget();
            _playedTargetName = _lockedTarget is not null && IsEligible(_lockedTarget) ? _lockedTarget.Title : null;
        }
        return Task.CompletedTask;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/no_return");
        if (_lockedTarget is null || !IsEligible(_lockedTarget)) return;
        _resolvingCard = _lockedTarget;
        try { await CardCmd.AutoPlay(context, _resolvingCard, null); }
        finally { _resolvingCard = null; }
    }
    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay,
        ResourceInfo resources, CardLocation location) => card == _resolvingCard
        ? new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom) : location;
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
