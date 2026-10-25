using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class AGiftOfWheels : NewsanguoCardTemplate
{
    public AGiftOfWheels() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { }
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/AGiftOfWheels.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<FourWheeledCart>(IsUpgraded)];
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("CartName", ModelDb.Card<FourWheeledCart>().Title + (IsUpgraded ? "+" : ""));
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var recipient = play.Target?.Player;
        if (recipient is null || recipient == Owner || !recipient.Creature.IsAlive) return;
        NewsanguoSfx.Play("event:/newsanguo/sfx/a_gift_of_wheels");
        var cart = CombatState!.CreateCard<FourWheeledCart>(recipient);
        if (IsUpgraded) CardCmd.Upgrade(cart);
        var result = await CardPileCmd.AddGeneratedCardToCombat(cart, PileType.Hand, recipient);
        CardCmd.PreviewCardPileAdd(result, 1.2f);
    }
}
