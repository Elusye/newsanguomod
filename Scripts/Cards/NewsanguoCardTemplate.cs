using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ValueProps;
using newsanguo.Scripts.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 新三国卡牌的统一基类。子类可通过重写 <see cref="IsScryCard"/> 来显示“预见”关键词。
/// 注意：子类若重写 <see cref="CanonicalKeywords"/>，必须把 <c>base.CanonicalKeywords</c> 合并进去，
/// 否则本模板附加的“预见”等模组关键词会丢失。
/// （“募集”不是模组关键词，而是自建的悬停提示——模组关键词的标题是静态文案、
/// 显示不了卡面数值，见 Scripts/Cards/RecruitKeyword.cs。）
/// </summary>
public abstract class NewsanguoCardTemplate : ModCardTemplate
{
    private const string ScryKeywordId = "NEWSANGUO_KEYWORD_SCRY";
    private const string TigerDragonKeywordId = "NEWSANGUO_KEYWORD_TIGER_DRAGON";

    public virtual bool IsTigerDragonCard => false;

    // 自身效果结算后统一触发；先快照敌人，避免死亡改变枚举集合。
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this || !IsTigerDragonCard || CombatState is null)
        {
            return;
        }
        foreach (var enemy in CombatState.HittableEnemies.ToList())
        {
            DragonOmenPower? omen = enemy.GetPower<DragonOmenPower>();
            if (!enemy.IsAlive || omen is null || omen.Amount <= 0)
            {
                continue;
            }
            await CreatureCmd.Damage(choiceContext, enemy, omen.Amount,
                ValueProp.Unblockable | ValueProp.Unpowered,
                dealer: null, cardSource: null, cardPlay: cardPlay);
        }
    }

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
            if (IsTigerDragonCard &&
                ModKeywordRegistry.TryGet(TigerDragonKeywordId, out ModKeywordDefinition tigerDragonDefinition))
            {
                yield return tigerDragonDefinition.CardKeywordValue;
            }
        }
    }
}
