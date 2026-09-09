using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 新三国诅咒牌的统一基类
/// 统一约定：诅咒类型/稀有度/无目标、不可升级、不可被 modifiers随机生成、
/// 子类仍需自行：加 [RegisterCard(typeof(CurseCardPool))]、在构造函数传费用、
/// 定义 CanonicalKeywords 与回合末触发等专属机制。
/// </summary>
public abstract class NewsanguoCurseTemplate : NewsanguoCardTemplate
{
    protected NewsanguoCurseTemplate(int energyCost)
        : base(energyCost, CardType.Curse, CardRarity.Curse, TargetType.None)
    {
    }

    // 诅咒牌不能升级
    public override int MaxUpgradeLevel => 0;

    // 诅咒牌不参与 modifiers（事件/遗物等）随机生成
    public override bool CanBeGeneratedByModifiers => false;

}
