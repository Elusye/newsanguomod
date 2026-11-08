using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public sealed class SelfControl : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/relics/SelfControl.png",
        IconOutlinePath: "res://newsanguo/images/relics/SelfControlOutline.png",
        BigIconPath: "res://newsanguo/images/relics/SelfControlBig.png"
    );

    private bool _anyAttacksPlayedThisTurn;

    private bool AnyAttacksPlayedThisTurn
    {
        get => _anyAttacksPlayedThisTurn;
        set
        {
            AssertMutable();
            _anyAttacksPlayedThisTurn = value;
        }
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner != cardPlay.Player || !CombatManager.Instance.IsInProgress || cardPlay.Card.Type != CardType.Attack)
        {
            return Task.CompletedTask;
        }

        Status = RelicStatus.Normal;
        AnyAttacksPlayedThisTurn = true;
        return Task.CompletedTask;
    }

    public override bool ShouldFlush(Player player)
    {
        return player != Owner || AnyAttacksPlayedThisTurn;
    }

    public override Task AfterEnergyReset(Player player)
    {
        if (player != Owner) return Task.CompletedTask;
        Status = RelicStatus.Active;
        AnyAttacksPlayedThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side || !participants.Contains(Owner.Creature)) return Task.CompletedTask;

        if (!AnyAttacksPlayedThisTurn)
        {
            Flash();
        }
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        Status = RelicStatus.Normal;
        AnyAttacksPlayedThisTurn = false;
        return Task.CompletedTask;
    }
}
