using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Keywords;
namespace newsanguo.Scripts.Cards;

/// <summary>地域牌不可打出，持有即生效；通过同步右键操作将其消耗。</summary>
public abstract class TerritoryCardTemplate : NewsanguoCardTemplate, IModRightClickableCard
{
    public override bool CanBeGeneratedInCombat => false;
    protected override bool IsPlayable => false;
    protected TerritoryCardTemplate(int cost, CardRarity rarity) : base(cost, CardType.Skill, rarity, TargetType.Self) { }
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (var keyword in base.CanonicalKeywords) yield return keyword;
            if (ModKeywordRegistry.TryGet("NEWSANGUO_KEYWORD_TERRITORY", out var definition))
                yield return definition.CardKeywordValue;
        }
    }
    internal static bool HasTerritoryKeyword(CardModel card) =>
        ModKeywordRegistry.TryGet("NEWSANGUO_KEYWORD_TERRITORY", out var definition) &&
        card.Keywords.Contains(definition.CardKeywordValue);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    public override bool ShouldPlay(CardModel card, AutoPlayType type) => card != this;
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Task.CompletedTask;

    private bool CanExhaustFromHand() => IsMutable && Pile?.Type == PileType.Hand &&
        Owner.Creature.IsAlive && Owner.PlayerCombatState?.Phase == PlayerTurnPhase.Play &&
        !CombatManager.Instance.IsOverOrEnding;
    public bool CanHandleRightClickLocal(ModRightClickContext context) =>
        IsMutable && context.Player == Owner && CanExhaustFromHand();
    public bool CanExecuteRightClick(ModRightClickExecutionContext context) =>
        IsMutable && context.Player == Owner && CanExhaustFromHand();
    public Task OnRightClick(ModRightClickExecutionContext context) =>
        CanExecuteRightClick(context) && context.PlayerChoiceContext is { } choice
            ? CardCmd.Exhaust(choice, this) : Task.CompletedTask;
}
