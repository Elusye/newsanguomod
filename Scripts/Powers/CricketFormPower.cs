using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “蛐蛐形态”：每 N 个玩家回合开始时将你的难以杀灭（hard_to_kill）层数翻倍，
/// N = 本能力层数（打出的蛐蛐形态张数，即 Amount）。
/// 定位为<b>负面状态（Debuff）</b>：可被清除负面效果的手段移除。
/// 采用“天降雄兵”同款双数字显示：
/// 右下角（Amount）= N，即每经过多少个回合翻倍；
/// 右上角（IHasSecondAmount）= 距下一次翻倍还剩多少个回合。
/// 打出蛐蛐形态时两个数字同时 +1；每个玩家回合开始时右上角 -1，归零当回合翻倍并把右上角恢复为 N。
/// </summary>
[RegisterPower]
public class CricketFormPower : ModPowerTemplate, IHasSecondAmount
{
    private class Data
    {
        // 距下一次翻倍还剩的玩家回合开始次数（图标右上角第二数字）
        public int turnsLeft = 0;
    }

    // 描述变量：剩余回合数（供 powers.json 描述中的 {TurnsLeft} 使用）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("TurnsLeft", 0)
    ];

    // 负面效果（Debuff）：
    //  · 图标与悬停提示按负面样式渲染（HoverTip.IsDebuff = Type == Debuff，HoverTip.cs:118）；
    //  · 会被“移除自身所有负面效果”一类手段清掉——本 mod 的「破除万杯」按
    //    TypeForCurrentAmount == PowerType.Debuff 筛选（BrewHealsAll.cs:70-76）。
    // 注意两点（都已确认不影响本能力）：
    //  · 原版对“玩家侧的 Debuff”会设置 SkipNextDurationTick（PowerCmd.cs:144-147），
    //    那是给按回合递减的能力用的；本能力的剩余回合由自己的 turnsLeft 计数，不受影响。
    //  · 原版 ArtifactPower 会拦下任何施加给自己的 Debuff（含自己给的，PowerCmd 的 applier 被判空忽略），
    //    但 0.107 里玩家没有任何获得 Artifact 的途径（只有怪物给自己上），所以自加蛐蛐形态不会被挡。
    public override PowerType Type => PowerType.Debuff;
    // 计数器：右下角由原版直接显示 Amount（N = 每几个回合翻倍一次）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 玩家回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 打出一张蛐蛐形态：右下角层数（Amount = 打出张数）已由 PowerCmd 累加，
    // 这里让右上角“距下次翻倍还剩的回合数”也随打出 +1
    public void RegisterCopy()
    {
        GetInternalData<Data>().turnsLeft++;
        SyncDisplay();
    }

    // 能力图标右上角第二数字：距下一次翻倍还剩多少个回合
    public string GetSecondAmount()
    {
        return GetInternalData<Data>().turnsLeft.ToString();
    }

    // 同步剩余回合数到描述变量并刷新图标右上角数字
    private void SyncDisplay()
    {
        int turnsLeft = GetInternalData<Data>().turnsLeft;
        if (DynamicVars.TryGetValue("TurnsLeft", out DynamicVar turnsLeftVar))
        {
            turnsLeftVar.BaseValue = turnsLeft;
        }
        this.InvokeSecondAmountChanged();
    }

    // 玩家回合开始时：右上角剩余回合数 -1；归零当回合翻倍难以杀灭，并把剩余回合数恢复为 N（Amount）
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.turnsLeft--;
        if (data.turnsLeft > 0)
        {
            SyncDisplay();
            return;
        }

        // 到达翻倍回合：先翻倍，再把右上角恢复为 N
        await DoubleHardToKill(choiceContext);
        data.turnsLeft = Amount;
        SyncDisplay();
    }

    // 将玩家的难以杀灭层数翻倍（上限999）
    private async Task DoubleHardToKill(PlayerChoiceContext choiceContext)
    {
        HardToKillPower? hardToKill = Owner.GetPower<HardToKillPower>();
        if (hardToKill is null || hardToKill.Amount <= 0)
        {
            return;
        }

        int delta = hardToKill.Amount;
        int maxDelta = 999 - hardToKill.Amount;
        if (delta > maxDelta)
        {
            delta = maxDelta;
        }
        if (delta <= 0)
        {
            return;
        }

        // 触发“蛐蛐形态”音效（对应 FMOD 事件 event:/newsanguo/sfx/cricket_form_power）
        NewsanguoSfx.Play("event:/newsanguo/sfx/cricket_form_power");

        await PowerCmd.ModifyAmount(choiceContext, hardToKill, delta, Owner, null, silent: false);
    }
}
