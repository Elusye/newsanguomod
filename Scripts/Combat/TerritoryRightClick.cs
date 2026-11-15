using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interactions.RightClick;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Combat;

/// <summary>右键仅发送原版战斗卡牌 ID，实际消耗统一进入同步行动队列。</summary>
public sealed class TerritoryRightClick : IModRightClickHandler
{
    public static void Register() => ModRightClickRegistry.Register(new TerritoryRightClick());

    public int Priority => 100;
    public bool TryHandle(ModRightClickContext context)
    {
        if (context.Model is not TerritoryCardTemplate card || !card.IsMutable ||
            card.Owner != context.Player || card.Pile?.Type != PileType.Hand)
            return false;
        TerritoryHandAction.Request(card, destinationIndex: -1);
        // 消费本次右键，不再由通用模型右键接口发送第二份动作。
        return true;
    }
}
