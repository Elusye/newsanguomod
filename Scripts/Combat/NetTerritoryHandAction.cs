using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Combat;

// Concrete INetAction types in mod assemblies are discovered by ActionTypes.Initialize.
public struct NetTerritoryHandAction : INetAction
{
    internal uint CardId;
    internal int DestinationIndex;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(CardId);
        writer.WriteInt(DestinationIndex);
    }

    public void Deserialize(PacketReader reader)
    {
        CardId = reader.ReadUInt();
        DestinationIndex = reader.ReadInt();
    }

    public GameAction ToGameAction(Player player) => new TerritoryHandAction(player, this);
}

internal sealed class TerritoryHandAction(Player player, NetTerritoryHandAction message) : GameAction
{
    public override ulong OwnerId => player.NetId;
    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;
    public override bool RecordableToReplay => true;
    public override INetAction ToNetAction() => message;

    internal static void Request(CardModel card, int destinationIndex)
    {
        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
            card.Owner.PlayerCombatState?.Phase != PlayerTurnPhase.Play || !card.Owner.Creature.IsAlive ||
            card.Pile?.Type != PileType.Hand)
        {
            return;
        }

        var action = new NetTerritoryHandAction
        {
            CardId = NetCombatCardDb.Instance.GetCardId(card),
            DestinationIndex = destinationIndex
        };
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new TerritoryHandAction(card.Owner, action));
    }

    protected override async Task ExecuteAction()
    {
        var combat = player.PlayerCombatState;
        if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
            combat?.Phase != PlayerTurnPhase.Play || !player.Creature.IsAlive ||
            !NetCombatCardDb.Instance.TryGetCard(message.CardId, out CardModel? card) ||
            card is not TerritoryCardTemplate || card.Owner != player || !combat.Hand.Cards.Contains(card))
        {
            return;
        }

        if (message.DestinationIndex == -1)
        {
            await CardCmd.Exhaust(new GameActionPlayerChoiceContext(this), card);
        }
        else if (message.DestinationIndex >= 0)
        {
            TerritoryHandOrder.ApplyMove(card, combat.Hand, message.DestinationIndex);
        }
    }
}
