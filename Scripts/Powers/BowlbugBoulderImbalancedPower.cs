using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Monsters;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// 盛碗虫（巨石）的“失衡”标记：与原版 ImbalancedPower 行为完全相同 ——
/// 当持有者（这只怪）的攻击被目标完全格挡时，把它标记为失衡，于是它下一回合会打眩晕招式。
///
/// 为什么不用原版 ImbalancedPower：它把持有者硬编码成 BowlbugRock
/// （ImbalancedPower.cs:22 的 `if (!(base.Owner.Monster is BowlbugRock bowlbugRock))` 会走通用眩晕分支），
/// 我们的怪换了类名，用原版能力会走 else 分支、拿不到 IsOffBalance 那套眩晕演出，
/// 所以这里自带一份同逻辑的实现。
///
/// 文案照搬原版：localization/*/powers.json 里的
/// NEWSANGUO_POWER_BOWLBUG_BOULDER_IMBALANCED_POWER.* 与游戏内 IMBALANCED_POWER.* 逐字一致。
/// </summary>
[RegisterPower]
public class BowlbugBoulderImbalancedPower : ModPowerTemplate
{
    // 负面标记（怪物身上的负面状态，与原版一致）
    public override PowerType Type => PowerType.Debuff;
    // 单层：存在即生效
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标：直接复用原版“失衡”的图标（原版图标按 id 命名：res://images/powers/<id小写>.png）。
    // 想换成自己的图，把这两个路径指到 res://newsanguo/images/powers/… 即可。
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/powers/imbalanced_power.png",
        BigIconPath: "res://images/powers/imbalanced_power.png"
    );

    // 持有者攻击被完全格挡 → 失衡
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner || !result.WasFullyBlocked)
        {
            return;
        }

        Flash();

        if (Owner.Monster is BowlbugBoulder boulder)
        {
            boulder.IsOffBalance = true;
            return;
        }

        // 兜底：挂在别的怪身上时走原版的通用眩晕
        await CreatureCmd.Stun(Owner);
    }
}