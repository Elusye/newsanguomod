using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
public class where_s_wine_power : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示每次酒力减少时抽的牌数
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要酒力变化的战斗钩子
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

    // 在酒力层数变化后，如果增加了则抽牌
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        if (power is not drunken_might || power.Owner != Owner) return;
        if (Amount <= 0) return;

        int gainedAmount = power.Amount - _drunkenMightAmountBeforeChange;
        if (gainedAmount > 0)
        {
            // 获得酒力触发音效
            NewsanguoSfx.Play("event:/newsanguo/sfx/where_s_wine_power");

            // 抽 Amount 张牌
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
        }
    }
}
