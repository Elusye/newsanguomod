using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace newsanguo.Scripts.Patches;

/// <summary>万花筒分别从角色池抽牌时，排除同一组奖励已选中的共享牌 ID。</summary>
[HarmonyPatch(typeof(Kaleidoscope), nameof(Kaleidoscope.AfterObtained))]
public static class KaleidoscopeRewardPatch
{
    public static bool Prefix(Kaleidoscope __instance, ref Task __result)
    {
        __result = OfferRewards(__instance);
        return false;
    }

    private static async Task OfferRewards(Kaleidoscope relic)
    {
        Player player = relic.Owner;
        var rewards = new List<Reward>();
        var rerollOptions = CardCreationOptions.ForNonCombatWithDefaultOdds(System.Array.Empty<CardPoolModel>());
        for (int rewardIndex = 0; rewardIndex < relic.DynamicVars.Cards.IntValue; rewardIndex++)
        {
            var cards = new List<CardModel>();
            var usedIds = new HashSet<string>();
            var pools = player.UnlockState.CharacterCardPools
                .Where(pool => pool != player.Character.CardPool).ToList()
                .StableShuffle(player.RunState.Rng.Niche).Take(3);
            foreach (CardPoolModel pool in pools)
            {
                var options = new CardCreationOptions(new[] { pool }, CardCreationSource.Other,
                    CardRarityOddsType.RegularEncounter)
                    .WithFlags(CardCreationFlags.NoCardPoolModifications)
                    .WithFilter(card => !usedIds.Contains(card.Id.Entry));
                CardModel card = CardFactory.CreateForReward(player, 1, options).First().Card;
                cards.Add(card);
                usedIds.Add(card.Id.Entry);
            }
            rewards.Add(new CardReward(cards, CardCreationSource.Other, player, rerollOptions));
        }
        await RewardsCmd.OfferCustom(player, rewards);
    }
}
