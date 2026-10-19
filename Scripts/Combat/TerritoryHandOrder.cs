using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using STS2RitsuLib.Networking.ManagedActions;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Combat;

/// <summary>通过原版动作队列同步地域牌换位，禁止只改本地显示顺序。</summary>
public static class TerritoryHandOrder
{
    internal const int GuchengMoveEnergyCost = 1;

    private readonly record struct Move(uint CardId, int Index);
    private static readonly RitsuLibManagedNetActionDescriptor<Move> Descriptor = new(
        Entry.ModId, "territory_hand_reorder", message =>
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, message.CardId);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), message.Index);
            return bytes;
        }, bytes => new Move(BinaryPrimitives.ReadUInt32LittleEndian(bytes),
            BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4))), async context =>
        {
            var player = context.Player;
            var hand = player.PlayerCombatState?.Hand;
            if (hand is null || player.PlayerCombatState!.Phase != PlayerTurnPhase.Play ||
                CombatManager.Instance.IsOverOrEnding ||
                !NetCombatCardDb.Instance.TryGetCard(context.Message.CardId, out CardModel? card) ||
                card is not TerritoryCardTemplate || card.Owner != player || !hand.Cards.Contains(card))
                return;
            // 原版内部列表保留实体顺序，不模拟离手/入手，以免触发抽牌及弃牌效果。
            var cards = (System.Collections.Generic.IList<CardModel>)hand.Cards;
            int currentIndex = cards.IndexOf(card);
            int targetIndex = Math.Clamp(context.Message.Index, 0, cards.Count - 1);
            // 拖回原位不收费；队列执行时再次检查能量，防止连续拖拽超支。
            if (targetIndex == currentIndex)
                return;
            if (card is InvincibleWithinThreeHundredLi)
            {
                if (player.PlayerCombatState!.Energy < GuchengMoveEnergyCost)
                    return;
                await PlayerCmd.LoseEnergy(GuchengMoveEnergyCost, player);
            }
            cards.Remove(card);
            cards.Insert(targetIndex, card);
            hand.InvokeContentsChanged();
            var ui = NPlayerHand.Instance;
            if (ui is not null)
            {
                var holders = ui.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToList();
                int position = 0;
                foreach (var model in hand.Cards)
                {
                    var holder = holders.FirstOrDefault(h => h.CardNode?.Model == model);
                    if (holder is not null) ui.CardHolderContainer.MoveChild(holder, position++);
                }
                ui.ForceRefreshCardIndices();
            }
            foreach (var model in hand.Cards) model.BaseReplayCount = model.BaseReplayCount;
            return;
        }, GameActionType.CombatPlayPhaseOnly);
    public static void Register() => RitsuLibManagedNetActions.Register(Descriptor);
    public static void Request(CardModel card, int index) => RitsuLibManagedNetActions.Request(null,
        Descriptor, new Move(NetCombatCardDb.Instance.GetCardId(card), index), card.Owner.NetId);
}
