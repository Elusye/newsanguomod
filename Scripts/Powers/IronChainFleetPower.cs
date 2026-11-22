using MegaCrit.Sts2.Core.Combat;
using newsanguo.Scripts.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class IronChainFleetPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/IronChainFleetPower.png",
        BigIconPath: "res://newsanguo/images/powers/IronChainFleetPowerBig.png");

    private IEnumerable<CardPlay> OwnPlaysNewestFirst() =>
        CombatManager.Instance.History.CardPlaysStarted.Reverse()
            .Select(entry => entry.CardPlay)
            .Where(play => play.Player.Creature == Owner);

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (!Owner.IsAlive || cardPlay.Player.Creature != Owner)
            return;

        var previous = OwnPlaysNewestFirst().SkipWhile(play => play != cardPlay).Skip(1).FirstOrDefault();
        if (previous?.Card.Type == cardPlay.Card.Type)
            await HeavensForce.Gain(context, Owner.Player!, Amount, cardPlay.Card);
    }

    internal void AddPreviousCardType(LocString description)
    {
        var previous = OwnPlaysNewestFirst().FirstOrDefault();
        description.Add("HasPreviousCard", previous is not null);
        description.Add("PreviousCardType", previous?.Card.Type.ToLocString().GetFormattedText() ?? "");
    }
}
