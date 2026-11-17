using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class VacationCountdown : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;
    public override bool IsAllowed(IRunState runState) => false;
    public override bool ShowCounter => true;
    public override int DisplayAmount => 3 - CompletedBattles;
    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/relics/Portal.png",
        IconOutlinePath: "res://newsanguo/images/relics/PortalOutline.png",
        BigIconPath: "res://newsanguo/images/relics/PortalBig.png");

    [SavedProperty]
    public int CompletedBattles { get; set; }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (CompletedBattles >= 3) return;
        CompletedBattles++;
        InvokeDisplayAmountChanged();
        if (CompletedBattles < 3) return;
        Flash();
        await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(Owner).ToMutable(), Owner);
        await RelicCmd.Remove(this);
    }
}
