using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
using newsanguo.Scripts.Combat;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天意之力”的逻辑载体：不显示在能力栏（数值改由副资源 <see cref="HeavensForce"/> 显示）。
/// 只负责两件事：
/// 玩家回合结束时：若数值不小于10，执行一次原“天意之助”效果（1层“双倍伤害”+1个额外回合），然后数值-10；
/// 若数值不大于-10，获得1层“天意侵蚀”，然后数值+10。每次最多转化10点（例如33 → 23），余数保留继续累计。
/// 本能力由 <see cref="HeavensForce"/> 在数值首次不为 0 时自动挂载，并在整个战斗期间保留；
/// 同时兼作“本场战斗累计失去的天意之力”账本（“天意修正”按此结算额外伤害）。
/// </summary>
[RegisterPower]
public class HeavensForcePower : ModPowerTemplate
{
    // 标记本回合结束是否触发了正向转化（授予额外回合），引擎随后询问 ShouldTakeExtraTurn 时读取并清除
    private bool _grantExtraTurn;

    // 本场战斗累计失去的天意之力（不含额外回合转化时的内部扣减）
    public int LostThisCombat { get; private set; }

    // 本场战斗累计获得的天意之力（不含天意侵蚀转化时的内部回升）
    public int GainedThisCombat { get; private set; }

    // 记账：卡牌/能力使天意之力减少时累加
    public void RecordLoss(int amount)
    {
        if (amount > 0)
        {
            LostThisCombat += amount;
        }
    }

    // 记账：卡牌/能力使天意之力增加时累加
    public void RecordGain(int amount)
    {
        if (amount > 0)
        {
            GainedThisCombat += amount;
        }
    }

    // 逻辑载体：不显示在能力栏
    protected override bool IsVisibleInternal => false;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    // 允许接收战斗钩子，否则 AfterSideTurnEnd / ShouldTakeExtraTurn 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 图标资源：与“天意之力”副资源（HeavensForce）共用同一组图，位于 secondary_resource 目录
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/secondary_resource/HeavensForce.png",
        BigIconPath: "res://newsanguo/images/secondary_resource/HeavensForceBig.png"
    );

    // 玩家回合结束时：每回合最多转化一次（10层换1次效果），余数保留
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }

        int amount = HeavensForce.Get(Owner.Player);
        if (amount == 0)
        {
            return;
        }

        if (amount >= 10)
        {
            // 天意之力正向转化触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_force）
            NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_force");

            // 正向转化：执行一次原“天意之助”效果（1层“双倍伤害”+1个额外回合）。
            // 注意：不能在这里先把数值-10——扣减会改变副资源数值，若恰好10点则扣到0，
            // 引擎随后询问 ShouldTakeExtraTurn 时状态已被打乱。因此扣数值推迟到
            // AfterTakingExtraTurn（额外回合被确认授予后）再进行。
            _grantExtraTurn = true;
            await PowerCmd.Apply<DoubleDamagePower>(choiceContext, Owner, 1, Owner, null, silent: false);
        }
        else if (amount <= -10)
        {
            // 负向转化时播放全屏特效（肾上腺素，提示天意侵蚀发作）
            VfxCmd.PlayFullScreenInCombat("vfx/vfx_adrenaline", Owner);

            // 天意之力负向转化触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_force_decay）
            NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_force_decay");

            // 负向转化：获得1层天意侵蚀，天意之力+10（例如-33 → -23；-10 → 0）
            // 这 10 点属于内部转化结算，不计入“本场获得的天意之力”账本
            await PowerCmd.Apply<HeavensDecayPower>(choiceContext, Owner, 1, Owner, null, silent: false);
            await HeavensForce.Gain(choiceContext, Owner.Player, 10, recordGain: false);
        }
    }

    // 引擎在玩家回合结束后询问是否获得额外回合（正向转化时返回true，参考原版遗物“佩尔之眼”）
    public override bool ShouldTakeExtraTurn(Player player)
    {
        if (player?.Creature != Owner)
        {
            return false;
        }
        bool grant = _grantExtraTurn;
        _grantExtraTurn = false;
        return grant;
    }

    // 引擎确认授予额外回合后，才扣除本次正向转化消耗的10点（属于内部转化结算，不计入“本场失去的天意之力”账本）
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player?.Creature != Owner)
        {
            return;
        }
        await HeavensForce.Lose(new ThrowingPlayerChoiceContext(), player, 10, recordLoss: false);
    }
}
