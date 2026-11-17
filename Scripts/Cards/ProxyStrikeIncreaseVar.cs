using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace newsanguo.Scripts.Cards;

/// <summary>瞄准时预览本场战斗内获得的意图伤害加成。</summary>
public class ProxyStrikeIncreaseVar : DynamicVar
{
    // 卡面文案里引用的名字
    public const string VarName = "IncreaseBy";

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
