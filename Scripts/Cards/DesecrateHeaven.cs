using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class DesecrateHeaven : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的天意之力、每回合开始时失去的天意之力
    // （后者作为“天意致胜”的负层数施加，基础 5 层）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(15m),
        new PowerVar<VictoryByHeavensWillPower>(5m)
    ];

    // 鼠标悬停时显示天意之力与天意侵蚀提示（“保留”关键词由引擎自动补充）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public DesecrateHeaven() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/desecrate_heaven");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得天意之力
        int heavensForceAmount = DynamicVars["HeavensForcePower"].IntValue;
        await HeavensForce.Add(choiceContext, base.Owner, heavensForceAmount, this);

        // 在本回合保留手牌（原版“保留手牌”能力）
        await PowerCmd.Apply<RetainHandPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);

        // 施加 -5 层“天意致胜”：每回合开始时失去 5 点天意之力（重复打出可叠加）
        int loss = DynamicVars["VictoryByHeavensWillPower"].IntValue;
        await PowerCmd.Apply<VictoryByHeavensWillPower>(
            choiceContext,
            base.Owner.Creature,
            -loss,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 升级后获得“保留”关键词
        AddKeyword(CardKeyword.Retain);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Retain);
    }
}
