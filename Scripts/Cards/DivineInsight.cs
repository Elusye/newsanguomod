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
    // 失去的天意之力用 IntVar（而非 PowerVar<HeavensForcePower>）：它是“失去”数值，
    // 不应参与 PowerVar 的卡面预览钩子，否则会被“换大盏”等加成钩子错误地加高显示
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("HeavensLost", 7),
        new PowerVar<DivineInsightPower>(1m)
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public DivineInsight() : base(0, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/divine_insight");

        // 先失去 7 点天意之力（HeavensForcePower 允许负值，可透支/结余为负）
        await PowerCmd.Apply<HeavensForcePower>(
            choiceContext,
            base.Owner.Creature,
            -DynamicVars["HeavensLost"].IntValue,
            base.Owner.Creature,
            this);

        // 再获得“参悟天意”能力：每打出一张牌，获得对应点数的天意之力（可叠加）。
        // 无需记录来源卡：能力在本次出牌结算中才生效，本次出牌不在能力的账本里，自然不会触发
        await PowerCmd.Apply<DivineInsightPower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["DivineInsightPower"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);
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
