using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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
public class FatherCanClaimTheThrone : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：施加 1 层“称帝”（每回合开始时获得 1 点能量并额外抽 1 张牌），
    // 并施加 -5 层“天意致胜”（每回合开始时失去 5 点天意之力）。
    // father_can_claim_the_throne_power 变量仅作描述展示：表示施加给“天意致胜”的负层数大小（升级 5 → 4）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VictoryByHeavensWillPower>("father_can_claim_the_throne_power", 5),
        new EnergyVar(1),
        new IntVar("draw_count", 1)
    ];

    // 鼠标悬停时显示天意之力与天意侵蚀提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    public FatherCanClaimTheThrone() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/father_can_claim_the_throne");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 施加 1 层“称帝”：每回合开始时获得 1 点能量并额外抽 1 张牌（重复打出可叠加层数）
        await PowerCmd.Apply<FatherCanClaimTheThronePower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this,
            silent: false);

        // 施加“天意致胜”负层数：每回合开始时失去与层数相同的天意之力（基础 5 层，升级 4 层）
        int loss = DynamicVars["father_can_claim_the_throne_power"].IntValue;
        await PowerCmd.Apply<VictoryByHeavensWillPower>(
            choiceContext,
            base.Owner.Creature,
            -loss,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级：每回合失去的天意之力减少（施加的“天意致胜”负层数 5 → 4）
    protected override void OnUpgrade()
    {
        DynamicVars["father_can_claim_the_throne_power"].UpgradeValueBy(-1);
    }
}
