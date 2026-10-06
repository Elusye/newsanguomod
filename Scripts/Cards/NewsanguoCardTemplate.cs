using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Patches;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 新三国卡牌的统一基类。子类可通过重写 <see cref="IsScryCard"/> 来显示“预见”关键词。
/// 注意：子类若重写 <see cref="CanonicalKeywords"/>，必须把 <c>base.CanonicalKeywords</c> 合并进去，
/// 否则本模板附加的“预见”等模组关键词会丢失。
/// </summary>
public abstract class NewsanguoCardTemplate : ModCardTemplate
{
    private const string ScryKeywordId = "NEWSANGUO_KEYWORD_SCRY";

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
    /// 卡框/卡底色取自 <see cref="CardModel.FrameMaterial"/>，而它等于
    /// <see cref="CardModel.VisualCardPool"/>.FrameMaterial，原版 VisualCardPool 直接返回
    /// <see cref="CardModel.Pool"/>，也就是“ModelDb 中第一个包含该牌 id 的卡池”。
    /// 我们的牌同时登记在新三国池与蜀汉池里，于是永远先命中新三国池 → 蜀汉的牌也一直是棕色。
    /// 这里改成跟随打出这张牌的角色所属卡池：曹魏 → 新三国池（棕），蜀汉 → 蜀汉池（墨绿）。
    /// 注意：卡牌图鉴里的原型牌是"规范模型"（不可变），访问 <see cref="CardModel.Owner"/>
    /// 会触发 CanonicalModelException（AssertMutable 抛"Canonical model … used in incorrect place"），
    /// 所以只在可变实例上按角色取池；图鉴那条路径改用"玩家当前浏览的卡池"上下文
    /// （见 <see cref="CardLibraryPoolContext"/>，由 <see cref="CardLibraryPoolFilterPatch"/> 维护），
    /// 其余场合一律回退到原版行为。
    /// </summary>
    public override CardPoolModel VisualCardPool
    {
        get
        {
            if (!IsMutable)
            {
                return CardLibraryPoolContext.CurrentPool ?? base.VisualCardPool;
            }

            return Owner?.Character?.CardPool ?? base.VisualCardPool;
        }
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

    /// <summary>
    /// 每当你失去天意之力时调用（数值真的减少之后由
    /// <see cref="newsanguo.Scripts.Combat.HeavensForce"/> 统一派发，覆盖“卡牌/效果造成的失去”
    /// 与“额外回合的转化扣减”两条路径）。默认什么都不做，
    /// “参见汉中王！”用它把自己放回手牌。
    /// </summary>
    public virtual Task OnHeavensForceLost(PlayerChoiceContext choiceContext, int amount)
    {
        return Task.CompletedTask;
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

            // 2026-10-04：“禁术牌”关键词已按要求整体删除
            // （原实现：IsForbiddenSpell 为 true 时 yield 出 NEWSANGUO_KEYWORD_FORBIDDEN_SPELL）
        }
    }
}
