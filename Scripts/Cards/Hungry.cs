using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册到诅咒卡池（与其他诅咒牌一起，可供诅咒奖励/事件获取）
[RegisterCard(typeof(CurseCardPool))]
public class Hungry : NewsanguoCurseTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 关键词：虚无（回合结束若在手牌则自行消耗）+ 不可打出（对应文本由引擎自动追加）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Unplayable];

    // 回合结束时若这张牌在手牌中，引擎会调用 OnTurnEndInHand
    public override bool HasTurnEndInHandEffect => true;

    public Hungry() : base(-1)
    {
    }

    // 回合结束时：这张牌若在手牌中，你下个回合少抽1张牌
    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/hungry");
        
        // 原版“下回合抽牌”能力支持负层数：-1 即下回合少抽1张（随后因“虚无”自行消耗）
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, base.Owner.Creature, -1, base.Owner.Creature, this);
    }
}
