using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天下无敌”：若你没有飞行（flight），则对没有振翅和翱翔（soar）的敌人造成伤害增加 Amount%。
/// 可叠加：Amount = 叠加次数 × 10（升级牌每次 15）。
/// </summary>
[RegisterPower]
public class invincible_power : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，可叠加
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 伤害修正（乘法）：若你没有飞行，则对“非振翅/翱翔”敌人造成的攻击伤害 ×（100 + Amount）%
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer != Owner || !props.IsPoweredAttack())
        {
            return 1m;
        }
        if (Owner?.HasPower<flight_power>() == true)
        {
            return 1m;
        }
        if (target is null || target.HasPower<FlutterPower>() || target.HasPower<SoarPower>())
        {
            return 1m;
        }

        return (100m + Amount) / 100m;
    }
}
