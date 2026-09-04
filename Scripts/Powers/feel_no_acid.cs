using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

// 注册能力到游戏
[RegisterPower]
public class feel_no_acid : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：单层（效果不随层数提升，不显示层数）
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 允许接收战斗钩子（否则 TryModifyPowerAmountReceived 不会被调用）
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 酒力每次只会减少 1 点：将酒力的负数变化量截断为 -1
    // 该钩子会在酒力层数变化前被引擎调用（与原版 ArtifactPower 抵消 debuff 同一机制）
    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        if (Owner is not null && target == Owner && canonicalPower is drunken_might && amount < 0)
        {
            modifiedAmount = Math.Max(amount, -1m);
            // 触发音效：酒力被削减时（咱家不怕酸生效）
            // 对应 FMOD 事件 event:/newsanguo/sfx/feel_no_acid_power，需在 FMOD 中补齐后重新导出 bank
            NewsanguoSfx.Play("event:/newsanguo/sfx/feel_no_acid_power");
            return true;
        }

        modifiedAmount = amount;
        return false;
    }
}
