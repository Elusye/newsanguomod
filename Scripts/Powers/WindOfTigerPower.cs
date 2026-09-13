using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “风从虎，云从龙”：层数即“笑面虎”额外获得的格挡，以及“龙可是帝王之征啊”额外给予的“帝王之征”层数。
/// 效果实现参考原版“精准”（AccuracyPower）：由能力在结算处追加数值。
/// </summary>
[RegisterPower]
public class WindOfTigerPower : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示加成数值（每打出一次“风从虎，云从龙”叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要在格挡/能力层数结算时被咨询
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 笑面虎额外获得与层数等量的格挡
    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (cardSource is not SmilingTiger || cardSource.Owner?.Creature != base.Owner)
        {
            return 0m;
        }
        return base.Amount;
    }

    // 龙可是帝王之征啊额外给予与层数等量的帝王之征
    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (giver != base.Owner || power is not DragonOmenPower || cardSource is not DragonOmen)
        {
            return 0m;
        }
        return base.Amount;
    }
}
