using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public sealed class HumanFootrest : NewsanguoCardTemplate
{
    public HumanFootrest() : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly) { }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/HumanFootrest.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HeavensForce", 15m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HeavensForce.HoverTip()];

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var recipient = play.Target?.Player;
        if (recipient is null || recipient == Owner || !recipient.Creature.IsAlive) return;

        NewsanguoSfx.Play("event:/newsanguo/sfx/human_footrest");
        await HeavensForce.Gain(context, recipient, DynamicVars["HeavensForce"].IntValue, this);
        PlayerCmd.EndTurn(Owner, false, null);
    }
}
