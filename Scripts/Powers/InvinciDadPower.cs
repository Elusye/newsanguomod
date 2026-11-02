using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>霸无敌：持有者造成的伤害被完全格挡，就获得等同于层数的永久力量。</summary>
[RegisterPower]
public class InvinciDadPower : ModPowerTemplate
{
    // 正面效果（这是怪物自己的增益）
    public override PowerType Type => PowerType.Buff;
    // 可叠加：每层提供1点触发时获得的力量。
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 伤害钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源（newsanguo/images/powers/InvinciDadPower.png 与 InvinciDadPowerBig.png）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/InvinciDadPower.png",
        BigIconPath: "res://newsanguo/images/powers/InvinciDadPowerBig.png"
    );

    // 持有者（盛碗虫（石））打出的伤害被完全格挡 → 获得力量
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner is null || !Owner.IsAlive || dealer != Owner)
        {
            return;
        }

        // 与失衡能力一致，按原版完全格挡判定触发。
        if (!result.WasFullyBlocked || Amount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, cardSource);
    }

}
