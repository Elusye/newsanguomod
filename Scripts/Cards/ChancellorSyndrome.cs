using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using newsanguo.Scripts.Characters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public sealed class ChancellorSyndrome : NewsanguoCardTemplate
{
    private const decimal HealFraction = 0.30m;

    public ChancellorSyndrome() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/ChancellorSyndrome.png");

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
    protected override void AfterDowngraded() => RemoveKeyword(CardKeyword.Retain);

    private decimal HealAmount => Math.Floor(Owner.Creature.MaxHp * HealFraction);

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("HealAmount", IsMutable && IsInCombat ? HealAmount : 0m);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.Heal(Owner.Creature, HealAmount);
        PlayerCmd.EndTurn(Owner, false, null);
    }
}
