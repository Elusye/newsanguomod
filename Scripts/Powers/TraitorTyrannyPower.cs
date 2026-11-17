using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

// 国贼：受到的攻击伤害增加 50%，没有回合递减。
[RegisterPower]
public class TraitorTyrannyPower : ModPowerTemplate
{
    // 负面效果
    public override PowerType Type => PowerType.Debuff;

    // 不可叠加，不显示层数
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool AllowNegative => false;

    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) =>
        target == Owner && props.IsPoweredAttack() ? 1.5m : 1m;
}
