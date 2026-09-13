using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 新三国卡牌的统一基类。子类可通过重写 <see cref="IsScryCard"/> 来显示“预见”关键词。
/// 注意：子类若重写 <see cref="CanonicalKeywords"/>，必须把 <c>base.CanonicalKeywords</c> 合并进去，
/// 否则本模板附加的“预见”“禁术牌”等模组关键词会丢失。
/// </summary>
public abstract class NewsanguoCardTemplate : ModCardTemplate
{
    private const string ScryKeywordId = "NEWSANGUO_KEYWORD_SCRY";
    private const string ForbiddenSpellKeywordId = "NEWSANGUO_KEYWORD_FORBIDDEN_SPELL";

    // 由“魔法禁术目录”遗物标记的回合数：该回合内打出这张牌不消耗天意之力（-1 表示未标记）
    private int _freeHeavensForceTurn = -1;

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

    /// <summary>
    /// 是否为“禁术牌”（牌名以“术”结尾的卡牌：人体炼成术、将领延寿术、心灵控制术、亡灵复活术）。
    /// 默认 false，需要在子类中显式重写。为 true 时卡牌会显示“禁术牌”关键词。
    /// </summary>
    public virtual bool IsForbiddenSpell => false;

    /// <summary>
    /// 本回合打出时是否不消耗天意之力。由“魔法禁术目录”遗物在回合开始时标记，
    /// 仅对该回合加入手牌的那张禁术牌生效，回合结束后自动失效。
    /// </summary>
    public bool IsFreeHeavensForceThisTurn =>
        _freeHeavensForceTurn == Owner?.PlayerCombatState?.TurnNumber;

    /// <summary>
    /// 标记为“本回合打出不消耗天意之力”。由“魔法禁术目录”遗物调用。
    /// </summary>
    public void SetToFreeHeavensForceThisTurn()
    {
        _freeHeavensForceTurn = Owner?.PlayerCombatState?.TurnNumber ?? -1;
    }

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

            if (IsForbiddenSpell &&
                ModKeywordRegistry.TryGet(ForbiddenSpellKeywordId, out ModKeywordDefinition forbiddenSpellDefinition))
            {
                yield return forbiddenSpellDefinition.CardKeywordValue;
            }
        }
    }
}
