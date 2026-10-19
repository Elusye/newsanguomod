using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>多层护甲：自身回合结束时获得层数格挡，攻击造成生命损失时减少一层。</summary>
[RegisterPower]
public class PlatedArmorPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    // 与原版 Plating 一致：在回合末伤害等效果之前获得格挡。
    public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !Owner.IsAlive || Amount <= 0 || !participants.Contains(Owner))
        {
            return;
        }
        Flash();
        // 能力格挡不受力量、灵巧等攻击/出牌加成影响。
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, cardPlay: null);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !Owner.IsAlive || result.UnblockedDamage <= 0 || !props.IsPoweredAttack())
        {
            return;
        }
        await PowerCmd.Decrement(this);
        if (Amount <= 0)
        {
            await PowerCmd.Remove(this);
        }
    }
}
