using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Relics;

namespace newsanguo.Scripts.Patches;

// 在原版检查选项数量前合并同一玩家的翅膀选项，保留每个实例的原始回调。
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyCardRewardAlternatives))]
public static class PaelsWingDuplicateAlternativePatch
{
    public static void Postfix(Player player, List<CardRewardAlternative> alternatives)
    {
        // 只匹配直接绑定到佩尔之翼的回调，独立修复模组已合并过的选项会被跳过。
        var wings = alternatives.Where(option => option.OptionId == PaelsWing.sacrificeAlternativeKey &&
            option.OnSelect.Target is PaelsWing wing && wing.Owner == player).ToArray();
        if (wings.Length < 2) return;
        int index = alternatives.IndexOf(wings[0]);
        var merged = new CardRewardAlternative(wings[0].OptionId, async () =>
        {
            // 顺序执行，确保每只翅膀独立计数并沿用原版的遗物 RNG 和动画。
            foreach (var option in wings) await option.OnSelect();
        }, wings[0].AfterSelected);
        foreach (var option in wings) alternatives.Remove(option);
        alternatives.Insert(index, merged);
    }
}
