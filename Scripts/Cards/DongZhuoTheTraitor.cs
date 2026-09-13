using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 国贼董卓嘛！：给予 1 层易伤，且目标的易伤不会在回合结束时减少
// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class DongZhuoTheTraitor : NewsanguoCardTemplate
{

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：立即给予的易伤层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(1m)
    ];

    // 鼠标悬停时显示“易伤”能力的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public DongZhuoTheTraitor() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效（FMOD 事件由 mod 维护者自行创建）
        NewsanguoSfx.Play("event:/newsanguo/sfx/dong_zhuo_the_traitor");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 立即给予易伤（层数取自卡牌变量）
        int vulnerable = DynamicVars.Vulnerable.IntValue;
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, vulnerable, base.Owner.Creature, this, silent: false);

        // 给予目标“国贼”能力：使其身上的易伤不再减少（含回合结束的自然衰减）
        await PowerCmd.Apply<TraitorTyrannyPower>(choiceContext, cardPlay.Target, 1, base.Owner.Creature, this, silent: false);
    }

    // 升级后的效果逻辑：费用 1 → 0
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
