using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

namespace newsanguo.Scripts.Combat;

/// <summary>
/// 自动选择卡牌时使用的选择器：照搬原版 VakuuCardSelector（Vakuu 通过“低语耳环”自动打牌用），
/// 仅将从左往右（options 正序取前 N 张）改为从右往左（options 逆序取前 N 张），
/// 与“天意侵蚀”从右到左自动打出手牌的方向保持一致。
/// 例如“赋值”这类牌在自动打出时若需要从手牌选择（变化/消耗/丢弃），会选中手牌最右边的一张/多张。
/// </summary>
public class RightToLeftCardSelector : ICardSelector
{
    /// <summary>
    /// 从候选中按“从右往左”的顺序选出至多 maxSelect 张。
    /// </summary>
    public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
    {
        return Task.FromResult((IEnumerable<CardModel>)options.Reverse().Take(maxSelect).ToList());
    }

    /// <summary>
    /// 从卡牌奖励候选中按“从右往左”的顺序选取最右侧的一张。
    /// </summary>
    public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
    {
        return new CardRewardSelection
        {
            card = options.LastOrDefault()?.Card
        };
    }
}
