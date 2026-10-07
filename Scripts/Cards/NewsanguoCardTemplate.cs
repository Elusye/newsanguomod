using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using newsanguo.Scripts.Characters;
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
    ///
    /// 2026-10-07（用户纠正）：衍生牌（<see cref="TokenCardPool"/>）、诅咒牌（<see cref="CurseCardPool"/>）、
    /// 状态牌（<see cref="StatusCardPool"/>）这三类原版共享卡池里的牌**不跟随角色**，
    /// 一律保留它们原本的原版卡框（无色灰 / 诅咒 / 无色灰）——它们跟着角色变成棕/墨绿才是异常。
    /// </summary>
    public override CardPoolModel VisualCardPool
    {
        get
        {
            // 原版 Pool：ModelDb 里第一个包含这张牌 id 的卡池
            CardPoolModel ownPool = base.VisualCardPool;

            if (KeepsVanillaFrame(ownPool))
            {
                return ownPool;
            }

            if (!IsMutable)
            {
                return CardLibraryPoolContext.CurrentPool ?? ownPool;
            }

            return Owner?.Character?.CardPool ?? ownPool;
        }
    }

    /// <summary>
    /// 这几类原版共享卡池里的本 mod 牌保留自己原本的原版卡框，不换角色卡框：
    /// 衍生牌 <see cref="TokenCardPool"/>（原版无色卡框）、诅咒牌 <see cref="CurseCardPool"/>（原版诅咒卡框）、
    /// 状态牌 <see cref="StatusCardPool"/>（原版无色卡框）。
    /// </summary>
    private static bool KeepsVanillaFrame(CardPoolModel pool)
    {
        return pool is TokenCardPool or CurseCardPool or StatusCardPool;
    }

    /// <summary>
    /// 卡面条件占位符用的变量名：与本地化文案里的 <c>{IsClone:…|}</c> 对应。
    /// </summary>
    protected const string IsCloneDescriptionArg = "IsClone";

    /// <summary>
    /// 往卡面描述里塞一个“本牌是不是复制品”的条件变量，
    /// 供文案里的 <c>{IsClone:打出复制品时的补充说明|}</c> 使用。
    ///
    /// 复制品（<see cref="CardModel.CreateClone"/> / <see cref="CardModel.CreateDupe"/>
    /// 产生的克隆，判断条件就是 <see cref="CardModel.IsClone"/>）生成时，引擎会清空它的
    /// <see cref="CardModel.DeckVersion"/>（<c>CardModel.AfterCloned</c>，sts2.decompiled.cs:74119-74140）。
    /// 因此“打出后让牌库本体成长”的牌（替身打击、比夷陵之火还好啊、医术高明）被打出的是复制品时，
    /// 成长只落在本场战斗的这张副本上，不会写回本体 —— 这一点必须在卡面上说清楚，
    /// 否则玩家会以为复制品也能刷成长。
    ///
    /// 写法参照原版「疯狂科学」（MadScience.cs:255-266：往描述里塞布尔变量）+ 卡面的条件占位符语法。
    /// </summary>
    protected void AddIsCloneDescriptionArg(LocString description)
    {
        description.Add(IsCloneDescriptionArg, IsClone);
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
