using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using STS2RitsuLib.Combat.AttackHits;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class UnclesAndAunts : NewsanguoCardTemplate, IAttackHitHookListener
{
    private AttackCommand? _currentAttack;
    private CardPlay? _currentPlay;

    // X 费卡（参考原版“天际钻头”HeavenlyDrill）
    protected override bool HasEnergyCostX => true;

    // 鼠标悬停时自动显示格挡提示（CardModel.HoverTips 依据此属性添加 StaticHoverTip.Block）
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(3m, ValueProp.Move),
        new DamageVar(4m, ValueProp.Move)
    ];

    // 无自带关键词：不再有“保留”。此处不重写 CanonicalKeywords，
    // 以免覆盖模板附加的模组关键词（NewsanguoCardTemplate 的合并逻辑只在基类里，见其文档注释）。

    // 悬停提示：卡面文案提到了力量与敏捷，只给这两项
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public UnclesAndAunts() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 将交叉加成接入原版的数值修正，让预览和实际结算共用同一个计算路径。
    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props,
        CardModel? cardSource, CardPlay? cardPlay) =>
        cardSource == this && target == Owner.Creature && props.IsPoweredCardOrMonsterMoveBlock()
            ? target.GetPowerAmount<StrengthPower>()
            : 0m;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        cardSource == this && dealer == Owner.Creature && props.IsPoweredAttack()
            ? dealer.GetPowerAmount<DexterityPower>()
            : 0m;

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/uncles_and_aunts");

        // 结算 X 的最终数值，升级后次数 +1（X 次 → X+1 次）
        var x = ResolveEnergyXValue();
        var repeat = x + (IsUpgraded ? 1 : 0);

        if (repeat <= 0) return;
        // 多段共用一个攻击命令，活力在所有段数结算完毕后消耗。
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(repeat)
            .WithHitFx("vfx/vfx_attack_slash");
        _currentAttack = attack;
        _currentPlay = cardPlay;
        try
        {
            await attack.Execute(choiceContext);
        }
        finally
        {
            _currentAttack = null;
            _currentPlay = null;
        }
    }

    // 每段仍按先格挡、后伤害的顺序结算。
    public Task BeforeAttackHit(AttackHitContext context) =>
        context.Attack == _currentAttack && _currentPlay is { } play
            ? CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play)
            : Task.CompletedTask;
}
