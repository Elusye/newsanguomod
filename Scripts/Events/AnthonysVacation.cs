using MegaCrit.Sts2.Core.Nodes.CommonUi;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Enchantments;
using newsanguo.Scripts.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

[RegisterActEvent(typeof(Glory))]
public sealed class AnthonysVacation : ModEventTemplate
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(5m), new HpLossVar(0m)];

    public override void CalculateVars()
    {
        // 用进入事件时的最大生命值计算，预览与实际扣血共用同一个整数。
        DynamicVars.HpLoss.BaseValue = (int)(Owner!.Creature.MaxHp * 0.35m);
    }

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: "res://newsanguo/images/events/AnthonysVacation.png");

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool eligible = Owner!.Deck.Cards.Count >= 20;
        return
        [
            new EventOption(this, eligible ? Hurry : null, InitialOptionKey(eligible ? "HURRY" : "HURRY_LOCKED"),
                HoverTipFactory.FromEnchantment<FineTuning>()),
            new EventOption(this, eligible ? LetSleep : null, InitialOptionKey(eligible ? "LET_SLEEP" : "LET_SLEEP_LOCKED"),
                HoverTipFactory.FromCard<Relax>()),
            new EventOption(this, eligible ? null : Wrath, InitialOptionKey(eligible ? "WRATH_LOCKED" : "WRATH"),
                HoverTipFactory.FromCard<TonyStrike>(), HoverTipFactory.FromCard<TonyDefend>()),
            new EventOption(this, eligible ? null : PleadForMercy, InitialOptionKey(eligible ? "MERCY_LOCKED" : "MERCY"))
                .ThatDoesDamage(DynamicVars.HpLoss.BaseValue - DynamicVars.MaxHp.BaseValue)
        ];
    }

    private Task Hurry()
    {
        var enchantment = ModelDb.Enchantment<FineTuning>();
        var candidates = Owner!.Deck.Cards.Where(enchantment.CanEnchant).ToList();
        var enchantedCards = new List<CardModel>(4);
        for (int i = 0; i < 4 && candidates.Count > 0; i++)
        {
            int index = Rng.NextInt(candidates.Count);
            var card = candidates[index];
            CardCmd.Enchant(enchantment.ToMutable(), card, 1m);
            enchantedCards.Add(card);
            candidates.RemoveAt(index);
        }
        CardCmd.Preview(enchantedCards, style: CardPreviewStyle.EventLayout);
        SetEventFinished(PageDescription("HURRY"));
        return Task.CompletedTask;
    }

    private async Task LetSleep()
    {
        var player = Owner!;
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(player.RunState.CreateCard<Relax>(player), PileType.Deck),
            style: CardPreviewStyle.EventLayout);
        await RelicCmd.Obtain<VacationCountdown>(player);
        SetEventFinished(PageDescription("LET_SLEEP"));
    }

    private async Task PleadForMercy()
    {
        var creature = Owner!.Creature;
        await CreatureCmd.GainMaxHp(creature, DynamicVars.MaxHp.BaseValue);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), creature, DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        SetEventFinished(PageDescription("MERCY"));
    }

    private async Task AddRandomCards(int count)
    {
        if (count <= 0) return;
        var player = Owner!;
        var candidates = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .DistinctBy(card => card.Id).ToArray();
        var results = new List<CardPileAddResult>();
        for (int i = 0; i < count && candidates.Length > 0; i++)
        {
            var card = player.RunState.CreateCard(candidates[Rng.NextInt(candidates.Length)], player);
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results, style: CardPreviewStyle.EventLayout);
    }

    private async Task Wrath()
    {
        var player = Owner!;
        await AddRandomCards(30 - player.Deck.Cards.Count);

        foreach (var card in player.Deck.Cards.ToArray())
        {
            if (card.Rarity != CardRarity.Basic) continue;
            CardModel? replacement = null;
            if (card.Tags.Contains(CardTag.Strike))
                replacement = player.RunState.CreateCard<TonyStrike>(player);
            else if (card.Tags.Contains(CardTag.Defend))
                replacement = player.RunState.CreateCard<TonyDefend>(player);
            if (replacement is null) continue;
            if (card.IsUpgraded) CardCmd.Upgrade(replacement);
            await CardCmd.Transform(card, replacement);
        }
        SetEventFinished(PageDescription("WRATH"));
    }
}
