using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册到诅咒卡池（与其他诅咒牌一起，可供诅咒奖励/事件获取）
[RegisterCard(typeof(CurseCardPool))]
public class Lightweight : NewsanguoCurseTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 关键词：不可打出（对应文本由引擎自动追加）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    // 悬停提示：展示“酒力”能力说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    public Lightweight() : base(-1)
    {
    }

    // 当酒力要施加到本方身上时，若这张牌此刻在本方手牌中，则将获得量压为 0（无法再获得酒力）。
    // 负向数值（失去酒力）不受影响；不损失酒力的“消耗”路径也照常。
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        // 只有拦截施加给本方（持有者）的“酒力”正向获得
        if (base.Owner?.Creature != target) return false;
        if (canonicalPower is not DrunkenMightPower) return false;
        if (amount <= 0m) return false;

        // 这张牌不在本方手牌中则不拦截（抽牌堆/弃牌堆/消耗堆均放行）
        if (base.Pile?.Type != PileType.Hand) return false;

        modifiedAmount = 0m;
        return true;
    }
}
