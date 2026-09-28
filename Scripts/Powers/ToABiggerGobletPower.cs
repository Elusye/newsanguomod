using System.Threading.Tasks;
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
public class ToABiggerGobletPower : ModPowerTemplate
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
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 当你获得酒力时，额外获得等同于换大盏层数的酒力。
    // 用 ModifyPowerAmountGivenAdditive（而非 TryModifyPowerAmountReceived）：
    // 原版 PowerVar<T>.UpdateCardPreview 会调用 ModifyPowerAmountGiven，
    // 因此手牌中的酒力数值会自动把这份加成算进卡面（打出时的实际结算同样走这条钩子）。
    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        return ShouldBoost(giver, amount, target, power) ? Amount : 0m;
    }

    // 实际结算时播放“换大盏”增强音效（卡面预览刷新不会走这里）
    public override Task AfterModifyingPowerAmountGiven(PowerModel power)
    {
        if (power is DrunkenMightPower)
        {
            NewsanguoSfx.Play("event:/newsanguo/sfx/to_a_bigger_goblet_power");
        }

        return Task.CompletedTask;
    }

    // 判断这次酒力获得是否应被“换大盏”增强
    private bool ShouldBoost(Creature giver, decimal amount, Creature? target, PowerModel power)
    {
        if (Owner is null) return false;
        // 只增强“获得”酒力（失去酒力时不加成）
        if (amount <= 0m) return false;
        if (power is not DrunkenMightPower) return false;

        // 归属判定：原版 Hook.ModifyPowerAmountGiven 会遍历全场模型（对方手牌/能力也在内），
        // 所以必须判干净，别把别人的酒力也算成自己的：
        //  · target 明确指定时以 target 为准 —— 盟友的“痛饮庆功酒”把酒力转给本方时同样应当加成；
        //  · target 为空时（原版 PowerVar.UpdateCardPreview 的卡面预览路径会传 null）用 giver 兜底，
        //    只有 giver 也是本方（= 正在预览本方的牌）才加成。
        // 与 Lightweight.ModifyPowerAmountGivenMultiplicative 的写法保持一致。
        if (target is not null)
        {
            return target == Owner;
        }
        return giver == Owner;
    }
}
