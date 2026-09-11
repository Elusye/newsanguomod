using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 新三国卡牌的统一基类。子类可通过重写 <see cref="IsScryCard"/> 来显示“预见”关键词。
/// </summary>
public abstract class NewsanguoCardTemplate : ModCardTemplate
{
    private const string ScryKeywordId = "NEWSANGUO_KEYWORD_SCRY";

    protected NewsanguoCardTemplate(
        int energyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool showInCardLibrary = true)
        : base(energyCost, type, rarity, targetType, showInCardLibrary)
    {
    }

    /// <summary>
    /// 是否带“预见”效果。默认 false，需要在子类中显式重写。
    /// 带预见的卡牌会显示“预见”关键词，悬停时展示预见机制说明。
    /// </summary>
    protected virtual bool IsScryCard => false;

    /// <summary>
    /// 是否属于“天意”体系（涉及天意之力/天意侵蚀的牌）。默认 false，需要在子类中显式重写。
    /// 供“恨天剑法”等按天意相关牌数量结算的效果统计使用。
    /// </summary>
    public virtual bool IsHeavensCard => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (CardKeyword keyword in base.CanonicalKeywords)
            {
                yield return keyword;
            }

            if (IsScryCard &&
                ModKeywordRegistry.TryGet(ScryKeywordId, out ModKeywordDefinition scryDefinition))
            {
                yield return scryDefinition.CardKeywordValue;
            }
        }
    }
}
