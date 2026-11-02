using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>敌方回合结束前，己方每名青州兵死亡给予 Amount 点天意之力。</summary>
[RegisterPower]
public class WorthAllTheirLivesPower : ModPowerTemplate
{
    private class Data
    {
        public readonly HashSet<Creature> soldiers = new();
    }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HeavensForce.HoverTip()];

    internal void TrackSoldier(Creature soldier) => GetInternalData<Data>().soldiers.Add(soldier);
    internal static bool TryConsumeSoldier(Creature owner, Creature soldier) =>
        owner.GetPower<WorthAllTheirLivesPower>()?.GetInternalData<Data>().soldiers.Remove(soldier) == true;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        Player? player = Owner.Player;
        if (wasRemovalPrevented || !Owner.IsAlive || player is null ||
            creature.Monster is not QingzhouSoldier || creature.PetOwner != player)
        {
            return;
        }
        Flash();
        await HeavensForce.Gain(choiceContext, player, Amount, this);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy)
            return;
        Data data = GetInternalData<Data>();
        foreach (Creature soldier in data.soldiers.Where(c => c.IsAlive && c.CombatState is not null).ToList())
            await CaoArtOfWarPower.DespawnSoldier(soldier);
        data.soldiers.Clear();
        await PowerCmd.Remove(this);
    }

}
