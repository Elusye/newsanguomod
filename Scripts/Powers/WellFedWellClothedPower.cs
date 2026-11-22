using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace newsanguo.Scripts.Powers;
[RegisterPower]
public class WellFedWellClothedPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/DrunkenMightPower.png",
        BigIconPath: "res://newsanguo/images/powers/DrunkenMightPowerBig.png");
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || !Owner.IsAlive) return;
        Flash();
        await PowerCmd.Apply<DrunkenMightPower>(context, Owner, Amount, Owner, null);
    }
}
