using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “霸无敌”（Invinci-Dad）：每当持有者（盛碗虫（石））造成的伤害被格挡时，
/// 它获得等同于玩家剩余[格挡]点数的[力量]（多人模式下取剩余格挡最高的那名玩家）。
///
/// 触发口径：
///  · “被格挡” = 这一次伤害至少有一点被格挡吸收（result.BlockedDamage &gt; 0，部分格挡也算；
///    注意 BlockedDamage 就是这次伤害真正打到格挡上的点数，见 Creature.DamageBlockInternal，
///    0 格挡时它是 0）。
///  · “剩余格挡点数” = 挨完这次伤害之后玩家还剩多少格挡 —— 引擎在伤害结算里先扣格挡再发钩子
///    （CreatureCmd.Damage → DamageBlockInternal 里 Block -= 吸收值），所以钩子里读到的 Block 已经是剩余值。
///  · 多人取最高 = 遍历本场所有存活玩家，取其中 Block 最大者；一个玩家都没有格挡时不获得力量。
///  · 力量给的是永久力量（走原版 StrengthPower），不是临时力量。
///
/// 本能力挂在盛碗虫（石）身上（dealer == Owner）。若要改成挂在玩家身上的“诅咒”，
/// 把钩子换成 AfterDamageReceived（dealer 为怪物时把力量给 dealer）即可。
///
/// 显示名/描述在 localization/*/powers.json 的 NEWSANGUO_POWER_INVINCI_DAD_POWER.* 里；
/// 图标是 newsanguo/images/powers/InvinciDadPower.png 与 InvinciDadPowerBig.png。
/// </summary>
[RegisterPower]
public class InvinciDadPower : ModPowerTemplate
{
    // 正面效果（这是怪物自己的增益）
    public override PowerType Type => PowerType.Buff;
    // 不叠加：存在即生效
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 伤害钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源（newsanguo/images/powers/InvinciDadPower.png 与 InvinciDadPowerBig.png）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/InvinciDadPower.png",
        BigIconPath: "res://newsanguo/images/powers/InvinciDadPowerBig.png"
    );

    // 持有者（盛碗虫（石））打出的伤害被格挡 → 获得力量
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

        // 这一次伤害至少被格挡吸收了一点（部分格挡也算）
        if (result.BlockedDamage <= 0)
        {
            return;
        }

        int strength = HighestPlayerBlock();
        if (strength <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, strength, Owner, cardSource);
    }

    // 多人模式取最高：所有存活玩家里剩余格挡最多的那个
    private int HighestPlayerBlock()
    {
        var combatState = Owner?.CombatState;
        if (combatState is null)
        {
            return 0;
        }

        int highest = 0;
        foreach (Player player in combatState.Players)
        {
            Creature? creature = player.Creature;
            if (creature is { IsAlive: true } && creature.Block > highest)
            {
                highest = creature.Block;
            }
        }

        return highest;
    }
}
