using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
namespace newsanguo.Scripts.Cards;
// 0.107兼容的出牌落点，不改变任何卡牌模型ID。
public sealed class CardLocation
{
    public Player player = null!;
    public PileType pileType;
    public CardPilePosition position;
}