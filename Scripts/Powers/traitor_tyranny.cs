using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “国贼”（国贼董卓嘛！施加）：可叠加，层数 = 每个敌方回合结束时给予的易伤层数。
/// 易伤在敌方回合结束时会自行衰减 1 层，本能力随后补回“层数”层，因此目标身上的易伤
/// 不会减少（层数 &gt; 1 时每回合还会净增长），且与原版易伤完全堆叠（同一个易伤图标与层数）。
/// </summary>
[RegisterPower]
public class traitor_tyranny : ModPowerTemplate
{
    // 负面效果
    public override PowerType Type => PowerType.Debuff;
    // 可叠加：Counter 显示层数（= 每回合结束时补给的易伤层数）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合结束钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源（复用原版“易伤”图标）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 敌方回合结束时，给予目标“层数”层易伤（与原版易伤层数堆叠）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy)
        {
            return;
        }
        if (Owner is null)
        {
            return;
        }

        // silent：自动补层不播放施放提示，避免每回合结束的视觉噪音
        await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner, Amount, null, null, silent: true);
    }
}
