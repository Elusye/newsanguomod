using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “钻石头冠（旧）”：你受到的攻击伤害减半，敌方回合结束时该效果消失。
/// 移植自原版「钻石头冠」遗物赋予的能力（原版类名 DiamondDiademPower），以便在本 mod 中复用。
/// </summary>
[RegisterPower]
public class OldDiamondDiademPower : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 开关型效果：不叠层，图标上也不显示层数
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 AfterSideTurnEnd 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 你受到的攻击伤害减半（乘法修正；中毒等 Unpowered 伤害不受影响）
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || !props.IsPoweredAttack())
        {
            return 1m;
        }

        return 0.5m;
    }

    // 敌方回合结束时移除自己：效果只覆盖敌方的一个回合
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.Remove(this);
        }
    }
}
