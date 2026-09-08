using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

// “国贼”（国贼董卓嘛！施加）：目标身上的易伤无法被减少。
// 阻断一切施加到目标身上的负向易伤变化（包括回合结束时的自然衰减与净化类移除）。
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
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 阻断目标受到的易伤减少
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        if (target == Owner && canonicalPower is VulnerablePower && amount < 0)
        {
            modifiedAmount = 0;
            return true;
        }

        modifiedAmount = amount;
        return false;
    }
}
