using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class OneForSorrowTwoForJoyPower : ModPowerTemplate
{
    [MegaCrit.Sts2.Core.Saves.Runs.SavedProperty]
    public bool IsJoyUpgraded { get; set; }
    public override MegaCrit.Sts2.Core.Localization.LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add("UpgradedJoy", IsJoyUpgraded);
            return description;
        }
    }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/OneForSorrowTwoForJoyPower.png",
        BigIconPath: "res://newsanguo/images/powers/OneForSorrowTwoForJoyPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Joy>(IsJoyUpgraded)];
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != Owner.Player || !Owner.IsAlive || Amount <= 0 ||
            card.Type is not (CardType.Status or CardType.Curse) || card.Pile?.Type != PileType.Hand ||
            Owner.CombatState is not { } combat) return;
        var replacement = combat.CreateCard<Joy>(card.Owner);
        if (IsJoyUpgraded) CardCmd.Upgrade(replacement);
        Flash();
        await CardCmd.Transform(card, replacement);
    }
}
