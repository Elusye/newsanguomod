using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “忽近忽远”：在接下来的回合内，交替获得与层数相同的临时敏捷和临时力量。
/// 多次打出此卡不会合并层数，而是创建独立的实例（PowerInstanceType.Instanced），
/// 各实例独立维护交替相位：
/// · 同一回合内连续打出多张：相位相同，等效合并（如两张 → 交替获得 6 力 6 敏）；
/// · 不同回合各自打出：相位互补，每回合同时获得力量与敏捷（两张 → 常驻 3 力 3 敏）。
/// </summary>
[RegisterPower]
public class near_and_far_power : ModPowerTemplate
{
    private class Data
    {
        // 本实例创建时的玩家回合号
        public int createdTurn = -1;
        // 下一个回合先获得敏捷，之后交替
        public bool nextIsDexterity = true;
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 多次打出创建独立实例，各自维持独立的交替节奏，不合并层数
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    // 层数即每次交替获得的量
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    private Data GetData() => GetInternalData<Data>();

    // 施加完成后确定本实例的交替相位：
    // · 同一回合内已打出的实例相位相同（合并效果，交替获得总层数）；
    // · 新回合打出的实例与最近一次打出的实例相位互补（各自独立，每回合都有力有敏）。
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        int currentTurn = Owner?.Player?.PlayerCombatState?.TurnNumber ?? -1;
        Data data = GetData();
        data.createdTurn = currentTurn;

        // 同回合已存在的实例：继承其相位
        near_and_far_power? sameTurn = Owner?.Powers.OfType<near_and_far_power>()
            .FirstOrDefault(p => p != this && p.GetData().createdTurn == currentTurn);
        if (sameTurn is not null)
        {
            data.nextIsDexterity = sameTurn.GetData().nextIsDexterity;
            return Task.CompletedTask;
        }

        // 新回合的第一张：与最近一次打出的实例反相
        near_and_far_power? previous = Owner?.Powers.OfType<near_and_far_power>()
            .Where(p => p != this && p.GetData().createdTurn >= 0)
            .OrderByDescending(p => p.GetData().createdTurn)
            .FirstOrDefault();
        if (previous is not null)
        {
            data.nextIsDexterity = !previous.GetData().nextIsDexterity;
        }
        return Task.CompletedTask;
    }

    // 回合开始时：交替获得临时敏捷/力量
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player is null || player.Creature != Owner || Amount <= 0)
        {
            return;
        }

        Flash();

        // 触发音效：交替获得临时敏捷/力量
        NewsanguoSfx.Play("event:/newsanguo/sfx/near_and_far_power");

        if (GetInternalData<Data>().nextIsDexterity)
        {
            await PowerCmd.Apply<near_and_far_dexterity_power>(choiceContext, Owner, Amount, Owner, null, silent: false);
        }
        else
        {
            await PowerCmd.Apply<near_and_far_strength_power>(choiceContext, Owner, Amount, Owner, null, silent: false);
        }

        // 翻转：下次获得另一种属性
        GetInternalData<Data>().nextIsDexterity = !GetInternalData<Data>().nextIsDexterity;
    }
}
