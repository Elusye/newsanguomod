using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace newsanguo.Scripts.Cards;
[RegisterCard(typeof(EventCardPool))]
public class TonyStrike : NewsanguoCardTemplate
{
    public TonyStrike() : base(1, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy) { }
    public override CardAssetProfile AssetProfile => new(PortraitPath: "res://newsanguo/images/cards/TonyStrike.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CalculationBaseVar(6m), new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier((card, _) => PileType.Deck.GetPile(card.Owner).Cards.Count)];
    protected override void OnUpgrade() => DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this, play).Targeting(play.Target).Execute(context);
    }
}
