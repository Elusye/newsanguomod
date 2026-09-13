using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class BrewLimitBreak : NewsanguoCardTemplate
{

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 鼠标悬停时显示酒力提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    public BrewLimitBreak() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/brew_limit_break");

        // 将酒力翻倍（没有酒力时无效果）
        if (base.Owner.Creature.GetPower<DrunkenMightPower>() is { } wine && wine.Amount > 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, wine, wine.Amount, base.Owner.Creature, this, silent: false);
        }
    }

    // 升级：去除消耗
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    // 降级：恢复消耗
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Exhaust);
    }
}
