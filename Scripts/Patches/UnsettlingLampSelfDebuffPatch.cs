using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Patches;

// 油灯被对敌易伤触发后不再检查目标阵营，导致同一卡牌的自身脆弱也翻倍。
// 仅修正无情剑法的自身代价，保留原版对敌施加负面效果的逻辑与触发次数。
[HarmonyPatch(typeof(UnsettlingLamp), nameof(UnsettlingLamp.ModifyPowerAmountGivenMultiplicative))]
public static class UnsettlingLampSelfDebuffPatch
{
    public static void Postfix(PowerModel power, Creature? target, CardModel? cardSource, ref decimal __result)
    {
        if (cardSource is RuthlessBlade { IsMutable: true } && power is FrailPower &&
            target is not null && target == cardSource.Owner?.Creature)
            __result = 1m;
    }
}
