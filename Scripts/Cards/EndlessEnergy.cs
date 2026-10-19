using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using MegaCrit.Sts2.Core.Models.CardPools;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(TokenCardPool))]
public class EndlessEnergy : TerritoryCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/EndlessEnergy.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. base.AdditionalHoverTips, HoverTipFactory.FromPower<FlightPower>(), HoverTipFactory.FromCard<Soldier>()
    ];
    public EndlessEnergy() : base(-1, CardRarity.Token) { }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext context, Player player)
    {
        // 等荆襄等普通回合开始效果结算后，再判定是否拥有飞行。
        if (player != Owner || !Owner.Creature.IsAlive || Pile?.Type != PileType.Hand ||
            CombatManager.Instance.IsOverOrEnding)
            return;
        NewsanguoSfx.Play("event:/newsanguo/sfx/endless_energy");
        if (Owner.Creature.GetPower<FlightPower>() is not { Amount: > 0 })
        {
            await CardPileCmd.Draw(context, DynamicVars.Cards.IntValue, Owner);
            return;
        }

        CardPile hand = PileType.Hand.GetPile(Owner);
        int missing = CardPile.MaxCardsInHand - hand.Cards.Count;
        if (missing > 0)
            await CardPileCmd.Draw(context, missing, Owner);
        // 抽牌堆和弃牌堆耗尽，或抽牌被阻止时，剩余空位用普通士兵填满。
        while (hand.Cards.Count < CardPile.MaxCardsInHand && Owner.Creature.IsAlive &&
            !CombatManager.Instance.IsOverOrEnding)
        {
            int before = hand.Cards.Count;
            var soldier = CombatState!.CreateCard<Soldier>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(soldier, PileType.Hand, Owner);
            // 外部效果若让新牌立即离手，避免无休止生成。
            if (hand.Cards.Count <= before) break;
        }
    }
}
