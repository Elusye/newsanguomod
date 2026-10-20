using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
using newsanguo.Scripts.Interop;
namespace newsanguo.Scripts.Powers;

// 注册能力到游戏
[RegisterPower]
public class WheresWinePower : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示每次获得酒力时抽的牌数
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要酒力变化的战斗钩子
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    public override LocString Description
    {
        get
        {
            LocString description = base.Description;
            SunqianUniverseVigorInterop.AddActiveResourceName(description, this);
            return description;
        }
    }

    // 在当前联动资源（酒力 / 活力）变化后，如果增加了则抽牌
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        if (power.Owner != Owner || !SunqianUniverseVigorInterop.IsActiveResourcePower(power, Owner)) return;
        if (Amount <= 0) return;

        // amount 是引擎已经修正过的本次变化量。不能读取共享旧值：
        // 失去资源触发「咱家不怕酸」时，嵌套的补偿事件会覆盖外层快照。
        if (amount > 0m)
        {
            // 获得酒力触发音效
            NewsanguoSfx.Play("event:/newsanguo/sfx/wheres_wine_power");

            // 抽 Amount 张牌
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
        }
    }
}
