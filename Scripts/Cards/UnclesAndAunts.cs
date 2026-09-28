using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

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

    // 卡牌基础数值：
    // 每次获得 3 点格挡 + 每点酒力额外 1 点（写法与「酒是老英雄」WineTheOldHero 一致：
    // CalculatedBlock = CalculationBase + CalculationExtra × 倍数，倍数 = 当前酒力点数），
    // 每次造成 4 点伤害（伤害侧的酒力加成由 DrunkenMightPower.ModifyDamageAdditive 自动生效）。
    // 次数 = X，升级后 X+1。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CalculationBaseVar(3m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(
            (card, _) => card.Owner?.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0),
        new DamageVar(4m, ValueProp.Move)
    ];

    // 无自带关键词：不再有“保留”。此处不重写 CanonicalKeywords，
    // 以免覆盖模板附加的模组关键词（NewsanguoCardTemplate 的合并逻辑只在基类里，见其文档注释）。

    // 悬停时补充“酒力”说明（卡面文案提到了酒力）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

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

        // 格挡数值只算一次：本张牌结算期间酒力不会变化（打出攻击牌后的减半在整张牌打完之后才结算），
        // 因此 X 轮里每轮获得的格挡都相同。
        // 注意必须走“数值 + ValueProp”这个重载：BlockVar 重载只取 BaseValue（= 3），会漏掉酒力加成。
        decimal blockAmount = DynamicVars.CalculatedBlock.Calculate(null);
        ValueProp blockProps = DynamicVars.CalculatedBlock.Props;

        // 每轮先获得格挡再造成伤害，重复“次数”轮。
        // 不再返还能量（原先的“找零 1 点能量”已移除）。
        for (var i = 0; i < repeat; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, blockProps, cardPlay);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }
}
