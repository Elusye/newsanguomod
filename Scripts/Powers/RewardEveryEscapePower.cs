using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class RewardEveryEscapePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/RewardEveryEscapePower.png",
        BigIconPath: "res://newsanguo/images/powers/RewardEveryEscapePowerBig.png");
    public override async Task AfterCardDiscarded(PlayerChoiceContext context, CardModel card)
    {
        if (Owner.Player is not { } player || card.Owner != player || !Owner.IsAlive ||
            CombatManager.Instance.IsOverOrEnding || Owner.CombatState?.CurrentSide != Owner.Side) return;
        Flash();
        await CardPileCmd.Draw(context, Amount, player);
    }
}
