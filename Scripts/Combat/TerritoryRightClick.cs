using System.Buffers.Binary;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Networking.ManagedActions;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Combat;

/// <summary>右键仅发送原版战斗卡牌 ID，实际消耗统一进入同步行动队列。</summary>
public sealed class TerritoryRightClick : IModRightClickHandler
{
    private static readonly RitsuLibManagedNetActionDescriptor<uint> Descriptor = new(
        Entry.ModId, "territory_exhaust", cardId =>
        {
            byte[] bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, cardId);
            return bytes;
        }, bytes => BinaryPrimitives.ReadUInt32LittleEndian(bytes), async context =>
        {
            if (!CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding ||
                context.Player.PlayerCombatState?.Phase != PlayerTurnPhase.Play ||
                !context.Player.Creature.IsAlive ||
                !NetCombatCardDb.Instance.TryGetCard(context.Message, out CardModel? card) ||
                card is not TerritoryCardTemplate || card.Owner != context.Player ||
                card.Pile?.Type != PileType.Hand)
                return;
            // 同一 ID 在首次执行后已离开手牌，重复请求自然被忽略。
            await CardCmd.Exhaust(context.PlayerChoiceContext, card);
        }, GameActionType.CombatPlayPhaseOnly);

    public static void Register()
    {
        RitsuLibManagedNetActions.Register(Descriptor);
        ModRightClickRegistry.Register(new TerritoryRightClick());
    }
    public int Priority => 100;
    public bool TryHandle(ModRightClickContext context)
    {
        if (context.Model is not TerritoryCardTemplate card || !card.IsMutable ||
            card.Owner != context.Player || card.Pile?.Type != PileType.Hand)
            return false;
        if (CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
            context.Player.PlayerCombatState?.Phase == PlayerTurnPhase.Play && context.Player.Creature.IsAlive)
            RitsuLibManagedNetActions.Request(null, Descriptor,
                NetCombatCardDb.Instance.GetCardId(card), context.Player.NetId);
        // 消费本次右键，不再由通用模型右键接口发送第二份动作。
        return true;
    }
}
