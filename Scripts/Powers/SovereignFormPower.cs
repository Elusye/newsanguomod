using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// 「君王形态」（<see cref="newsanguo.Scripts.SovereignForm"/>）施加的能力：
/// 你每获得 4 点酒力，就获得 <see cref="ModPowerTemplate.Amount"/> 点天意之力。
///
/// 2026-10-07（不可叠加改造）：<see cref="InstanceType"/> 改为
/// <see cref="PowerInstanceType.Instanced"/>（照抄原版「环绕」<c>OrbitPower</c>：:103194-103237，
/// 它同样用 Instanced + Counter + DisplayAmount 做“倒计时”图标），
/// 于是每张「君王形态」都是独立的一份能力：各自记自己的酒力进度、各自触发、互不影响；
/// 同一张牌打第二遍不会再叠到同一份能力上。
///
/// 图标双数字（沿用本 mod 已有的“天降雄兵 / 蛐蛐形态”机制）：
///  · 右上角（<see cref="IHasSecondAmount"/>）= <see cref="GetSecondAmount"/> = Amount
///    = 每触发一次给的天意之力点数（「君王形态」2 点，升级版 3 点）；
///  · 右下角（引擎原版 <c>%AmountLabel</c> 显示 <see cref="DisplayAmount"/>）
///    = <see cref="WinePerTrigger"/> − 已攒到的余数 = 还差多少点酒力才触发。
///
/// 2026-10-07：随卡牌改名，TheTrueKingPower → SovereignFormPower（本地化 key 同步为
/// NEWSANGUO_POWER_SOVEREIGN_FORM_POWER.*，音效改为 event:/newsanguo/sfx/sovereign_form_power）。
///
/// 触发判定照抄「何处有酒」（<see cref="WhereSWinePower"/>）的酒力获得钩子：
/// <see cref="BeforePowerAmountChanged"/> 记下变化前的层数，
/// <see cref="AfterPowerAmountChanged"/> 里用“变化后 − 变化前”算出本次真实获得的酒力
/// （这样「换大盏」等加成也算进累计，与实际到手点数一致）。
/// </summary>
[RegisterPower]
public class SovereignFormPower : ModPowerTemplate, IHasSecondAmount
{
    // 每累计获得这么多点酒力触发一次
    public const int WinePerTrigger = 4;

    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器（Amount 用于右上角显示；右下角显示的是 DisplayAmount 的倒计时）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不可叠加：每张「君王形态」各自一份独立实例（原版「环绕」OrbitPower 同款写法）
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要酒力变化的战斗钩子
    public override bool ShouldReceiveCombatHooks => true;

    // 右下角数字：还差多少点酒力才触发（原版用 DisplayAmount 显示在 %AmountLabel 上）
    public override int DisplayAmount => Math.Max(0, WinePerTrigger - _wineProgress);

    // 右上角数字（本 mod 的 IHasSecondAmount 机制）：每触发一次获得的天意之力点数
    public string GetSecondAmount()
    {
        return Amount.ToString();
    }

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 记录酒力变化前的层数
    private int _drunkenMightAmountBeforeChange;

    // 距离下一次触发还差的酒力累计（不满 4 点的余数留到下次继续攒）。
    // 故意只在战斗内记：能力本身随战斗结束消失，读档回到战斗中最多丢一次余数，无关大局。
    private int _wineProgress;

    // 在酒力层数变化前记录旧值
    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return Task.CompletedTask;
        if (power is DrunkenMightPower && target == Owner)
        {
            _drunkenMightAmountBeforeChange = power.Amount;
        }
        return Task.CompletedTask;
    }

    // 酒力真的增加后，按每 4 点一档换算成天意之力
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner is null) return;
        // 只认本方身上的酒力（失去酒力时 amount 为负，直接放行不入账）
        if (power is not DrunkenMightPower || power.Owner != Owner) return;
        if (amount <= 0m) return;

        int gainedAmount = power.Amount - _drunkenMightAmountBeforeChange;
        if (gainedAmount <= 0) return;

        _wineProgress += gainedAmount;

        // 可能一次进账就跨过多档（例如一口喝下 8 点酒力）
        int triggers = _wineProgress / WinePerTrigger;
        if (triggers > 0)
        {
            _wineProgress -= triggers * WinePerTrigger;

            // 触发音效（音频文件：newsanguo/audios/sovereign_form_power.mp3|wav|ogg）
            NewsanguoSfx.Play("event:/newsanguo/sfx/sovereign_form_power");

            // 获得天意之力（每档 Amount 点）
            await HeavensForce.Add(choiceContext, Owner.Player, triggers * Amount, null);
        }

        // 不管有没有触发，右下角的“还差几点酒力”都变了 ⇒ 刷新图标两个数字
        RefreshIcon();
    }

    // 刷新能力图标上的两个数字：
    //  · InvokeDisplayAmountChanged() → 引擎原版 NPower.OnDisplayAmountChanged → RefreshAmount()，
    //    更新右下角的 DisplayAmount（同时会闪一下图标，原版「环绕」也是每次进度变化就刷）；
    //  · InvokeSecondAmountChanged() → 本 mod SecondAmountLabelPatch 复制出的右上角标签。
    private void RefreshIcon()
    {
        InvokeDisplayAmountChanged();
        this.InvokeSecondAmountChanged();
    }
}
