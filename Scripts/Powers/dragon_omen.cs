using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “帝王之征”：层数标记能力。
/// 打出“龙可是帝王之征啊”时，拥有此能力的敌人失去与层数相等的生命。
/// </summary>
[RegisterPower]
public class dragon_omen : ModPowerTemplate
{
    // 负面效果
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：计数器，Amount 表示层数
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 纯标记能力，不需要战斗钩子
    public override bool ShouldReceiveCombatHooks => false;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );
}
