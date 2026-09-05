using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
    // 叠加方式：计数器，Amount 表示每次失去酒力时补偿的酒力数
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要酒力变化的战斗钩子（否则 Before/AfterPowerAmountChanged 不会被调用）
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 记录酒力变化前的层数
    private int _drunkenMightAmountBeforeChange;

    // 在酒力层数变化前记录旧值
    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return Task.CompletedTask;
        if (power is drunken_might && target == Owner)
        {
            _drunkenMightAmountBeforeChange = power.Amount;
        }
        return Task.CompletedTask;
    }

    // 在酒力层数变化后：每当你失去酒力时，获得与自身层数等量的酒力
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        if (power is not drunken_might || power.Owner != Owner) return;

        int lost = _drunkenMightAmountBeforeChange - power.Amount;
        if (lost <= 0) return;

        // 触发音效：失去酒力时（咱家不怕酸生效）
        NewsanguoSfx.Play("event:/newsanguo/sfx/feel_no_acid_power");

        // 获得 Amount 点酒力（gain 不会再触发本钩子，避免死循环）
        await PowerCmd.Apply<drunken_might>(choiceContext, Owner, base.Amount, Owner, null, silent: true);
    }
}
