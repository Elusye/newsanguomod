using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class CricketFormPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player) =>
        player.Creature == Owner && Owner.IsAlive ? DoubleHardToKill(choiceContext) : Task.CompletedTask;

    // 将玩家的难以杀灭层数翻倍（上限999）
    private async Task DoubleHardToKill(PlayerChoiceContext choiceContext)
    {
        OldHardToKillPower? hardToKill = Owner.GetPower<OldHardToKillPower>();
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
