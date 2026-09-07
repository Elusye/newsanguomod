using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class RuthlessBlade : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每次打击伤害、打击次数、目标虚弱层数、自身虚弱层数
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new RepeatVar(2),
        new PowerVar<WeakPower>("WeakPower", 1),
        new PowerVar<WeakPower>("SelfWeakPower", 1)
    ];

    // 悬停提示：展示“虚弱”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<WeakPower>()
    ];

    public RuthlessBlade() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/ruthless_blade");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 造成 8（10） 点伤害 2 次
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .Execute(choiceContext);

        // 给予目标 1 层虚弱，给予自身 1 层虚弱
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DynamicVars["WeakPower"].IntValue, base.Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(choiceContext, base.Owner.Creature, DynamicVars["SelfWeakPower"].IntValue, base.Owner.Creature, this);

        // 原版规则：给玩家施加的 Debuff 首次衰减会被跳过（SkipNextDurationTick = true），
        // 导致自身虚弱比敌方多持续一轮。这里显式取消跳过，使自身虚弱与敌方一样在下一个敌方回合结束正常衰减。
        WeakPower? selfWeak = base.Owner.Creature.GetPower<WeakPower>();
        if (selfWeak is not null)
        {
            selfWeak.SkipNextDurationTick = false;
        }
    }

    // 升级：每次打击伤害 5 → 6
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
    }
}
