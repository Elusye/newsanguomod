using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class DivineInsight : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 悬停提示：展示“天意之力”与“天意侵蚀”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 卡牌基础数值：打出时失去 7 点天意之力；每打出一张牌获得 1 点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("heavens_lost", 7),
        new PowerVar<DivineInsightPower>("divine_insight_power", 1)
    ];

    public DivineInsight() : base(0, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/divine_insight");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 先失去 7 点天意之力（HeavensForcePower 允许负值，可透支/结余为负）
        await PowerCmd.Apply<HeavensForcePower>(
            choiceContext,
            base.Owner.Creature,
            -DynamicVars["heavens_lost"].IntValue,
            base.Owner.Creature,
            this);

        // 再获得“参悟天意”能力：每打出一张牌，获得对应点数的天意之力（可叠加）
        // 记录来源卡，打出本卡自身时不触发
        DivineInsightPower? power = await PowerCmd.Apply<DivineInsightPower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["divine_insight_power"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);
        power?.MarkAppliedBy(this);
    }

    // 升级后的效果逻辑：获得“固有”
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）：移除“固有”
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Innate);
    }
}
