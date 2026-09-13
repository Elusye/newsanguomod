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

    // 卡牌基础数值：每次打击伤害、给予目标的易伤层数、给予自身的脆弱层数
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m),
        new PowerVar<FrailPower>(1m)
    ];

    // 悬停提示：展示“易伤”与“脆弱”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<FrailPower>()
    ];

    public RuthlessBlade() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌语音
        NewsanguoSfx.Play("event:/newsanguo/sfx/ruthless_blade");

        // 造成 5（6）点伤害 2 次
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_attack_slash", null, "heavy_attack.mp3")
            .Execute(choiceContext);

        // 给予目标 1 层易伤，给予自身 1 层脆弱
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars.Vulnerable.BaseValue, base.Owner.Creature, this);
        var frailInstance = await PowerCmd.Apply<FrailPower>(choiceContext, base.Owner.Creature, DynamicVars["FrailPower"].IntValue, base.Owner.Creature, this);

        // 原版规则：给玩家施加的 Debuff 首次衰减会被跳过（SkipNextDurationTick = true），
        // 导致自身脆弱比敌方易伤多持续一轮。这里显式取消跳过，使自身脆弱在下一个敌方回合正常衰减。
        if (frailInstance != null)
        {
            frailInstance.SkipNextDurationTick = false;
        }
    }

    // 升级：每次打击伤害 5 → 6，给予目标易伤 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}
