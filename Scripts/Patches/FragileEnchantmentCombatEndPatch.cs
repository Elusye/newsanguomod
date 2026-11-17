using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using newsanguo.Scripts.Enchantments;

namespace newsanguo.Scripts.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
public static class FragileEnchantmentCombatEndPatch
{
    public static void Prefix(IRunState runState, out CardModel[] __state)
    {
        // 只清理结算开始前已有的易碎牌。传世名典在结算中加入的牌留到下一场战斗。
        __state = runState.Players.SelectMany(player => player.Deck.Cards)
            .Where(card => card.Enchantment is Fragile).ToArray();
    }

    public static void Postfix(CardModel[] __state, ref Task __result) =>
        __result = RemoveFragileCards(__result, __state);

    private static async Task RemoveFragileCards(Task combatEnd, CardModel[] cards)
    {
        await combatEnd;
        foreach (var card in cards)
        {
            if (card.Pile?.Type == PileType.Deck && card.Enchantment is Fragile)
                await CardPileCmd.RemoveFromDeck(card);
        }
    }
}
