using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册到诅咒卡池（与其他诅咒牌一起，可供诅咒奖励/事件获取）
[RegisterCard(typeof(CurseCardPool))]
public class HeavensDecay : NewsanguoCurseTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 关键词：永恒（描述后自动追加“永恒。”）；可打出，不再有“不可打出”词条
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Eternal];

    // 悬停提示：牌名即“天意侵蚀”，而天意之力的说明文本中会出现“天意侵蚀”，两者须成对展示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 回合结束时若这张牌在手牌中，引擎会调用 OnTurnEndInHand
    public override bool HasTurnEndInHandEffect => true;

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public HeavensDecay() : base(2)
    {
    }

    // 打出时：消耗2费，但本身不产生任何效果（仅音效与施法动画）。
    // 回合结束时若仍留在手牌中，才会触发天意侵蚀。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
    }

    // 回合结束时：这张牌若在手牌中，天意爷会接管你的下个回合（获得1层天意侵蚀）
    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        // 播放天意侵蚀触发音效（与能力共用事件，GUIDs.txt 已有该事件）
        NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_decay");

        // 获得1层天意侵蚀：下个回合开始时天意爷接管，自动打出手牌
        await PowerCmd.Apply<HeavensDecayPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }
}
