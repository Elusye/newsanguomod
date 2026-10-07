using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Rewards;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// 「创造模式」能力：战斗结束时给拥有者加一张「创造模式」奖励，
/// 玩家在战斗奖励屏上点选它，即可从游戏里的全部卡牌中自选一张加入牌组（选牌逻辑见 CreativeModeReward）。
///
/// 钩子为什么可以挂在能力上：AfterCombatEnd 在 EndCombatInternal 里于 turnState.IsInProgress = false 之后、
/// 能力被移除之前调用（此刻 CombatManager.IsEnding 为 false），能力确实收得到——原版 ForbiddenGrimoirePower
/// 就是同款写法（那里给的是 CardRemovalReward）。原版给战斗结束收益的正统做法就是“额外奖励”，
/// 奖励会随战斗房间一起存读档。
///
/// 显示名/描述在 localization/*/powers.json 的 NEWSANGUO_POWER_CREATIVE_MODE_POWER.* 里（名字取自类名）；
/// 能力图标路径按类名取 res://newsanguo/images/powers/CreativeModePower.png（大图 …CreativeModePowerBig.png）。
/// </summary>
[RegisterPower]
public class CreativeModePower : ModPowerTemplate
{
    // 正面效果：Buff
    public override PowerType Type => PowerType.Buff;
    // 不叠加：存在即生效
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要战斗钩子（战斗结束钩子）
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 战斗结束时把「创造模式」奖励加进奖励屏（玩家可点选，也可跳过）
    public override Task AfterCombatEnd(CombatRoom room)
    {
        // 只有挂在玩家身上的能力才有效
        if (Owner?.Player is { } player)
        {
            room.AddExtraReward(player, new CreativeModeReward(player));
        }

        return Task.CompletedTask;
    }
}
