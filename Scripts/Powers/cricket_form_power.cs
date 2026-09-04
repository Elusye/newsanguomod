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
/// 采用“天降雄兵”同款双数字显示：
/// 右下角（Amount）= N，即每经过多少个回合翻倍；
/// 右上角（IHasSecondAmount）= 距下一次翻倍还剩多少个回合。
/// 打出蛐蛐形态时两个数字同时 +1；每个玩家回合开始时右上角 -1，归零当回合翻倍并把右上角恢复为 N。
/// </summary>
[RegisterPower]
public class cricket_form_power : ModPowerTemplate, IHasSecondAmount
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

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 计数器：右下角由原版直接显示 Amount（N = 每几个回合翻倍一次）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 玩家回合开始钩子需要战斗上下文
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
