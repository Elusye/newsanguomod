using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class GuanPingsProtectionPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png");

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 不检查生命损失，完全格挡的攻击和多段攻击的每一段也会反击。
        if (target != Owner || !props.IsPoweredAttack() || dealer is null || dealer.Side == Owner.Side ||
            !Owner.IsAlive || !dealer.IsAlive || Owner.Player is not { } player)
            return;
        int count = Amount;
        for (int i = 0; i < count; i++)
        {
            if (!Owner.IsAlive || !dealer.IsAlive || CombatManager.Instance.IsOverOrEnding)
                break;
            var pool = ModelDb.CardPool<ShuHanCardPool>()
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Type == CardType.Attack);
            CardModel? card = CardFactory.GetDistinctForCombat(player, pool, 1,
                player.RunState.Rng.CombatCardGeneration).FirstOrDefault();
            if (card is null)
                break;
            // 与「好厉害呀！」相同，打出引擎复制品，由引擎处理结算后的去向。
            card = card.CreateDupe(player);
            Flash();
            DrunkenMightPower.BeginRefundForCard(card);
            try
            {
                await CardCmd.AutoPlay(choiceContext, card, dealer);
            }
            finally
            {
                int refund = DrunkenMightPower.EndRefundForCard(card);
                if (refund > 0 && Owner.IsAlive && !CombatManager.Instance.IsOverOrEnding)
                    await PowerCmd.Apply<DrunkenMightPower>(choiceContext, Owner, refund, Owner, card);
            }
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
            await PowerCmd.Remove(this);
    }
}
