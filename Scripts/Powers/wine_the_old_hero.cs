using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

// 注册能力到游戏
[RegisterPower]
public class wine_the_old_hero : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示层数
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 允许接收战斗钩子
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

    // 在酒力层数变化后，如果减少了则按失去的酒力获得格挡
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        if (power is not drunken_might || power.Owner != Owner) return;
        if (Amount <= 0) return;

        int newAmount = power.Amount;
        int lostAmount = _drunkenMightAmountBeforeChange - newAmount;
        if (lostAmount > 0)
        {
            // 酒力减少触发音效（对应 FMOD 事件 event:/newsanguo/sfx/wine_the_old_hero_power）
            SfxCmd.Play("event:/newsanguo/sfx/wine_the_old_hero_power");

            // 每失去 1 点酒力获得 Amount 点格挡
            await CreatureCmd.GainBlock(Owner, lostAmount * Amount, ValueProp.Move, cardPlay: null, fast: false);
        }
    }
}
