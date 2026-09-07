using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class PoisonRat : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：给予 7 层中毒
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<PoisonPower>("PoisonPower", 7)
    ];

    // 悬停提示：展示“中毒”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<PoisonPower>()
    ];

    // 卡牌自带“虚无”和“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    public PoisonRat() : base(1, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/poison_rat");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 给予目标 7（10）层中毒
        await PowerCmd.Apply<PoisonPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["PoisonPower"].IntValue,
            base.Owner.Creature,
            this);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 中毒从 7 提高到 10
        DynamicVars["PoisonPower"].UpgradeValueBy(3);
    }
}
