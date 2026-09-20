using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// “四六征税”的预计金币收益（只用于卡面预览）。
/// 数值 = 目标当前血量 × TaxPercent% ÷ 游戏人数，与打出时的真实结算同源
/// （<see cref="FortySixtyTax.ComputeGold"/>，避免预览与结算两套口径漂移）。
///
/// 瞄准敌人时 <see cref="UpdateCardPreview"/> 会把该敌人的血量代入并写入
/// <see cref="DynamicVar.PreviewValue"/>；没有目标时（图鉴、抽牌堆浏览等）保持 0，
/// 描述里靠 <c>{IsTargeting:…|}</c> 把这一行整段隐藏。
///
/// 取值注意：描述必须写 <c>{TaxGold:diff()}</c> —— <c>diff</c> 格式化器读的是 PreviewValue；
/// 裸写 <c>{TaxGold}</c> 只会读到 BaseValue（恒为 0）。
/// </summary>
public class TaxGoldVar : DynamicVar
{
    public TaxGoldVar()
        : base("TaxGold", 0m)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);

        // 没指向任何敌人，或这张卡的税率变量缺失（理论上不会）时不显示收益
        if (target is null || !card.DynamicVars.TryGetValue("TaxPercent", out DynamicVar? taxPercent))
        {
            PreviewValue = 0m;
            return;
        }

        // 多人游戏金币会平分，CombatState 取不到时按单人算（与结算时的兜底一致）
        int playerCount = card.CombatState?.Players.Count ?? 1;
        PreviewValue = FortySixtyTax.ComputeGold(target, taxPercent.IntValue, playerCount);
    }
}
