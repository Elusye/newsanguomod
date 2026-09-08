using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class UnclesAndAunts : NewsanguoCardTemplate
{
    // X 费卡（参考原版“天际钻头”HeavenlyDrill）
    protected override bool HasEnergyCostX => true;

    // 鼠标悬停时自动显示格挡提示（CardModel.HoverTips 依据此属性添加 StaticHoverTip.Block）
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每次获得 3 点格挡、每次造成 4 点伤害（次数 = X，升级后 X+1）；
    // 另声明 1 点能量变量供卡面“获得{Energy:energyIcons()}”行渲染（返还 1 点能量）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(3m, ValueProp.Move),
        new DamageVar(4m, ValueProp.Move),
        new EnergyVar(1)
    ];

    // 升级前后都带“保留”关键词（升级后次数 X → X+1）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public UnclesAndAunts() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/uncles_and_aunts");

        // 结算 X 的最终数值，升级后次数 +1（X 次 → X+1 次）
        var x = ResolveEnergyXValue();
        var repeat = x + (IsUpgraded ? 1 : 0);

        // 每轮先获得格挡再造成伤害，重复“次数”轮
        for (var i = 0; i < repeat; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        // 找零：返还 1 点能量
        await PlayerCmd.GainEnergy(1, Owner);
    }
}
