using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 曹氏兵法：每当你打出一张牌时以 1 生命值独立募集 1 名青州兵；每当任何生物死亡时你获得 1 点力量；
// 在你的回合开始时移除所有青州兵（这次移除不触发能力）。可叠加，每层提高上述两个数值。
// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class CaoArtOfWar : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：打出一张牌时独立募集 1 名 1 生命值的青州兵；任何生物死亡时获得 1 点力量。
    // 两处数值都随打出的“曹氏兵法”数量叠加，由能力 cao_art_of_war_power 的层数体现。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new SummonVar(1),
        new PowerVar<StrengthPower>(1m)
    ];

    // 悬停提示：①“募集”说明 —— 本卡募集出来的青州兵会在你的回合开始时失去，这条说明里已写明。
    // 提示是自建的 HoverTip 而不是模组关键词：只有这样才能让标题里的数量跟随卡面数值
    // （本卡 Summon = 1 ⇒ 标题显示“募集1”），见 Scripts/Cards/RecruitKeyword.cs。
    // 2026-10-09 需求：青州兵的关键词由“召唤”改为“募集”，因此这里不再挂原版 SummonDynamic
    // 悬停提示 —— 那条提示讲的是“召唤奥斯提”，与本卡募集的青州兵无关。
    // ② 力量说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RecruitKeyword.Recruit(base.DynamicVars.Summon),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public CaoArtOfWar() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/cao_art_of_war");

        // 获得“曹氏兵法”能力（可叠加：每次打出令层数 +1，触发强度随之提高）。
        // 无需记录来源卡：能力在本次出牌结算中才生效，本次出牌不在能力的账本里，自然不会触发
        await PowerCmd.Apply<CaoArtOfWarPower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级：费用 2 → 1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
