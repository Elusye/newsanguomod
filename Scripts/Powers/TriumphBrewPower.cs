using System.Collections.Generic;
using System.Linq;
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
using newsanguo.Scripts.Interop;

namespace newsanguo.Scripts.Powers;

// 注册能力到游戏
[RegisterPower]
public class TriumphBrewPower : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：单一（同角色只能有一层该能力）
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要酒力变化的战斗钩子
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源（临时复用“换大盏”图标）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/ToABiggerGobletPower.png",
        BigIconPath: "res://newsanguo/images/powers/ToABiggerGobletPowerBig.png"
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

    // 连锁保护：某次“传播”进行期间，阻止任何“痛饮庆功酒”再次触发，避免盟友之间来回传播形成死循环
    private static bool _isPropagating;

    // 记录当前联动资源（酒力 / 活力）变化前的层数
    private int _resourceAmountBeforeChange;

    // 在酒力层数变化前记录旧值
    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return Task.CompletedTask;
        if (target == Owner && SunqianUniverseVigorInterop.IsActiveResourcePower(power, Owner))
        {
            _resourceAmountBeforeChange = power.Amount;
        }
        return Task.CompletedTask;
    }

    // 在酒力层数变化后，如果自己获得了酒力，则让其他盟友获得等量酒力
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        if (power.Owner != Owner || !SunqianUniverseVigorInterop.IsActiveResourcePower(power, Owner)) return;
        if (_isPropagating) return; // 这份酒力正来自其他盟友的“痛饮庆功酒”，不再继续传播

        int gainedAmount = power.Amount - _resourceAmountBeforeChange;
        if (gainedAmount <= 0) return;

        // 找出其他存活的玩家盟友
        var combatState = Owner.CombatState;
        if (combatState is null) return;

        List<Creature> allies = combatState.GetTeammatesOf(Owner)
            .Where(c => c != null && c != Owner && c.IsAlive && c.IsPlayer)
            .ToList();
        if (allies.Count == 0) return;

        _isPropagating = true;
        try
        {
            foreach (Creature ally in allies)
            {
                await SunqianUniverseVigorInterop.ApplyActiveResource(
                    choiceContext,
                    ally,
                    gainedAmount,
                    Owner,
                    null,
                    silent: true);
            }
        }
        finally
        {
            _isPropagating = false;
        }
    }
}
