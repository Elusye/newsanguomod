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

/// <summary>精甲：增加自身卡牌给予的格挡，自身回合开始时层数向下取整减半。</summary>
[RegisterPower]
public class FineArmorPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/FineArmorPower.png",
        BigIconPath: "res://newsanguo/images/powers/FineArmorPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        // 与敏捷一样在格挡乘算之前加成，但仅限自身卡牌，不增强能力等非卡牌格挡。
        return cardSource?.Owner?.Creature == Owner && props.IsPoweredCardOrMonsterMoveBlock()
            ? Amount : 0m;
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || !participants.Contains(Owner) || !Owner.IsAlive || Amount <= 0) return;
        Flash();
        await PowerCmd.ModifyAmount(choiceContext, this, Amount / 2 - Amount, Owner, null);
    }
}
