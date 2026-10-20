using System.Threading.Tasks;
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
public class FeelNoAcidPower : ModPowerTemplate
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

    // 在当前联动资源（酒力 / 活力）变化后：每当你失去它时，获得与自身层数等量的同种资源
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        if (power.Owner != Owner || !SunqianUniverseVigorInterop.IsActiveResourcePower(power, Owner)) return;

        // 只响应本次失去，补偿产生的正增量及其他嵌套事件不会再次触发。
        if (amount >= 0m) return;

        // 触发音效：失去酒力时（咱家不怕酸生效）
        NewsanguoSfx.Play("event:/newsanguo/sfx/feel_no_acid_power");

        // 获得 Amount 点同种资源；正增量会派发钩子，但不会再次通过上面的负增量判断。
        await SunqianUniverseVigorInterop.ApplyActiveResource(
            choiceContext,
            Owner,
            base.Amount,
            Owner,
            null,
            silent: true);
    }
}
