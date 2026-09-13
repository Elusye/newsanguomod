using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// “天意之力”消耗变量。实际消耗值（<see cref="DynamicVar.BaseValue"/>）始终不变，
/// 但当卡牌被“魔法禁术目录”遗物标记为本回合免消耗时，卡面显示值（<see cref="DynamicVar.PreviewValue"/>）改为 0。
/// 卡面描述需用 <c>inverseDiff()</c> 格式化该变量（而非 <c>diff()</c>），数值变低时才会显示为绿色。
/// </summary>
public class HeavensForceVar : PowerVar<HeavensForcePower>
{
    public HeavensForceVar(string name, decimal powerAmount)
        : base(name, powerAmount)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
        if (card is NewsanguoCardTemplate { IsFreeHeavensForceThisTurn: true })
        {
            PreviewValue = 0m;
        }
    }
}
