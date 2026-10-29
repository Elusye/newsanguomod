using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class DontBeAngryPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/DontBeAngryPower.png",
        BigIconPath: "res://newsanguo/images/powers/DontBeAngryPowerBig.png");
    public override async Task AfterDamageReceived(PlayerChoiceContext context, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack() || dealer is null || dealer.Side == Owner.Side ||
            !Owner.IsAlive || Owner.Player is not { } player) return;
        int count = Amount;
        for (int i = 0; i < count; i++)
        {
            if (!Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) break;
            var attacks = PileType.Exhaust.GetPile(player).Cards.Where(card => card.Type == CardType.Attack).ToList();
            if (attacks.Count == 0) break;
            var card = attacks[player.RunState.Rng.CombatCardGeneration.NextInt(attacks.Count)];
            Flash();
            // 打出消耗堆中的原牌；结算去向交由卡牌自身规则决定。
            await CardCmd.AutoPlay(context, card, null);
        }
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature == Owner) await PowerCmd.Remove(this);
    }
}
