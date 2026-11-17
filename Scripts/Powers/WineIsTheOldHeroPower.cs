using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Interop;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public sealed class WineIsTheOldHeroPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/WineIsTheOldHeroPower.png",
        BigIconPath: "res://newsanguo/images/powers/WineIsTheOldHeroPowerBig.png");

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            SunqianUniverseVigorInterop.AddActiveResourceName(description, this);
            return description;
        }
    }

    // 此时敌方回合已结束，玩家残余格挡尚未清除。
    public override async Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
            return;

        var owner = Owner;
        int block = owner.Block;
        int copies = Amount;
        await PowerCmd.Remove(this);
        for (int i = 0; i < copies && block > 0 && owner.IsAlive && !CombatManager.Instance.IsOverOrEnding; i++)
            await SunqianUniverseVigorInterop.ApplyActiveResource(context, owner, block, owner, null);
    }
}
