using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 「替身打击」卡面显示的“本次会永久增加多少伤害”（只用于卡面预览）。
/// 数值 = 目标本回合意图对这张牌主人造成的伤害 − 这张牌的伤害初始值（不足则 0），
/// 与打出时的真实结算同源（<see cref="ProxyStrike.ComputeIntentIncrease"/>，避免两套口径漂移）。
///
/// 瞄准敌人时 <see cref="UpdateCardPreview"/> 会把该敌人的意图代入并写入
/// <see cref="DynamicVar.PreviewValue"/>；没有目标时（图鉴、抽牌堆浏览等）保持 0，
/// 描述里靠 <c>{IsTargeting:…|}</c> 把这一行整段隐藏。
///
/// 取值注意：描述必须写 <c>{IncreaseBy:diff()}</c> —— <c>diff</c> 格式化器读的是 PreviewValue；
/// 裸写 <c>{IncreaseBy}</c> 只会读到 BaseValue（恒为 0）。
/// </summary>
public class ProxyStrikeIncreaseVar : DynamicVar
{
    // 卡面文案里引用的名字
    public const string VarName = "IncreaseBy";

    // 伤害初始值本身由 DamageVar / InitialDamage 持有，这个变量只负责“二者的差值”
    public ProxyStrikeIncreaseVar()
        : base(VarName, 0m)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        // 没指向任何敌人时不显示收益（描述里的这一段也会被 IsTargeting 隐藏）
        if (target is null)
        {
            PreviewValue = 0m;
            return;
        }

        PreviewValue = ProxyStrike.ComputeIntentIncrease(card, target);
    }
}
