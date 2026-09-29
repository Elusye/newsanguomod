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
/// 玩家回合结束时：若数值不小于 <see cref="ConversionAmount"/>，**询问玩家**是否做一次正向转化
/// （接受 = 1 层“双倍伤害”+1 个额外回合，然后数值 -10；拒绝 = 不执行任何操作：数值保留、不给双倍伤害、
/// 不进额外回合，只播一句「竟然不许！」语音，下个回合结束仍会再问）；
/// 若数值不大于 -10，获得1层“天意侵蚀”，然后数值+10。每次最多转化10点（例如33 → 23），余数保留继续累计。
/// 本能力由 <see cref="HeavensForce"/> 在数值首次不为 0 时自动挂载，并在整个战斗期间保留；
/// 同时兼作“本场战斗累计失去的天意之力”账本（“天意修正”按此结算额外伤害）。
/// </summary>
[RegisterPower]
public class HeavensForcePower : ModPowerTemplate
{
    /// <summary>
    /// 一次正向/负向转化涉及的点数（阈值、消耗与侵蚀回升量共用）。
    /// 卡面文案通过“顺应天意”那张选择卡的 <c>{ForceCost}</c> 引用这里，避免两处数字不同步。
    /// </summary>
    public const int ConversionAmount = 10;

    /// <summary>一次正向转化给予的“双倍伤害”层数（本次转化固定给 1 层）。</summary>
    public const int ConversionDoubleDamage = 1;

    // 拒绝转化时播放的语音：「竟然不许！」（音频文件 res://newsanguo/audios/heavens_force_decline.mp3；
    // NewsanguoSfx.Play 把事件路径的末段当文件名去找，支持未导入的裸 mp3）。
    // 拒绝本身**不改变任何游戏状态**（不扣点数、不给双倍伤害、不进额外回合），只播这句语音。
    private const string DeclineSfx = "event:/newsanguo/sfx/heavens_force_decline";

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
    //
    // 正向转化是**可选**的（参考原版「知识恶魔」的「知识的诅咒」：弹两张卡二选一）。
    // 注意：引擎询问“是否给额外回合”的 ShouldTakeExtraTurn 是**同步 bool**（CombatManager.cs:1366），
    // 没法在那里 await 玩家选择；所以选择必须在这里——本钩子带 choiceContext、可以 await——
    // 先问好，再把答案记进 _grantExtraTurn 供随后的 ShouldTakeExtraTurn 读取。
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

        if (amount >= ConversionAmount)
        {
            // 先问玩家要不要这次转化；拒绝（「竟然不许！」）**不执行任何操作**：
            // 点数保留在 ≥10，本回合不给双倍伤害、不进额外回合，下个回合结束仍会再问
            if (!await AskAboutHeavensAssist(choiceContext))
            {
                return;
            }

            // 天意之力正向转化触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_force）
            NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_force");

            // 正向转化：执行一次原“天意之助”效果（1层“双倍伤害”+1个额外回合）。
            // 注意：不能在这里先把数值-10——扣减会改变副资源数值，若恰好10点则扣到0，
            // 引擎随后询问 ShouldTakeExtraTurn 时状态已被打乱。因此扣数值推迟到
            // AfterTakingExtraTurn（额外回合被确认授予后）再进行。
            _grantExtraTurn = true;
            await PowerCmd.Apply<DoubleDamagePower>(choiceContext, Owner, ConversionDoubleDamage, Owner, null, silent: false);
        }
        else if (amount <= -ConversionAmount)
        {
            // 负向转化时播放全屏特效（肾上腺素，提示天意侵蚀发作）
            VfxCmd.PlayFullScreenInCombat("vfx/vfx_adrenaline", Owner);

            // 天意之力负向转化触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_force_decay）
            NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_force_decay");

            // 负向转化：获得1层天意侵蚀，天意之力+10（例如-33 → -23；-10 → 0）
            // 这 10 点属于内部转化结算，不计入“本场获得的天意之力”账本
            await PowerCmd.Apply<HeavensDecayPower>(choiceContext, Owner, 1, Owner, null, silent: false);
            await HeavensForce.Gain(choiceContext, Owner.Player, ConversionAmount, recordGain: false);
        }
    }

    /// <summary>
    /// 弹出「顺应天意 / 竟然不许！」两张选择卡，问玩家要不要做这次正向转化。
    /// 写法照抄原版「知识恶魔」的「知识的诅咒」（KnowledgeDemon.ChooseCurse）：
    /// 用 <c>CombatState.CreateCard</c> 在战斗内造出两张卡 → <c>CardSelectCmd.FromChooseACardScreen</c>
    /// 二选一（多人下每个玩家各自选，走 PlayerChoiceSynchronizer 同步）。
    /// 返回 true = 接受转化；false = 拒绝（「竟然不许！」，不执行任何操作）。
    /// </summary>
    private async Task<bool> AskAboutHeavensAssist(PlayerChoiceContext choiceContext)
    {
        // 战斗正在收尾（例如这一下已经打完最后一只怪）时不再弹窗，按“不接受”处理
        // 注意：本能力的 Owner 就是持有者生物（Creature），所以战斗状态直接从 Owner 取
        if (CombatManager.Instance.IsOverOrEnding || Owner.CombatState is not { } combatState)
        {
            return false;
        }

        List<CardModel> options =
        [
            combatState.CreateCard<HeavensForceAccept>(Owner.Player),
            combatState.CreateCard<HeavensForceDecline>(Owner.Player)
        ];

        CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner.Player);

        if (chosen is HeavensForceAccept)
        {
            return true;
        }

        if (chosen is null)
        {
            // 选择界面异常返回（canSkip: false 时正常必有结果）：按“不接受”处理，也不播语音
            return false;
        }

        // 「竟然不许！」：**不执行任何操作** —— 不扣点数、不给双倍伤害、不进额外回合，
        // 只播这句语音（纯表现，不影响任何游戏状态）。点数保留在 ≥10，下个回合结束仍会再问。
        NewsanguoSfx.Play(DeclineSfx);
        return false;
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
