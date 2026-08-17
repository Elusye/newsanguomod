using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “蛐蛐形态”：回合开始时，将你的难以杀灭（hard_to_kill）层数翻倍。
/// </summary>
[RegisterPower]
public class cricket_form_power : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 固定效果，无需层数
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 回合开始时：将玩家的难以杀灭层数翻倍
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner)
        {
            return;
        }

        HardToKillPower? hardToKill = Owner.GetPower<HardToKillPower>();
        if (hardToKill is null || hardToKill.Amount <= 0)
        {
            return;
        }

        // 翻倍，但难以杀灭层数上限999
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
        SfxCmd.Play("event:/newsanguo/sfx/cricket_form_power");

        await PowerCmd.ModifyAmount(choiceContext, hardToKill, delta, Owner, null, silent: false);
    }
}
