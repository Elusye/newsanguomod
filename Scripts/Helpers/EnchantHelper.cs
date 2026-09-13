using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Content;

namespace newsanguo.Scripts.Helpers;

/// <summary>
/// 随机附魔工具：从原版与本 mod 的附魔中随机挑选一个卡牌可用的附魔并施加。
/// 其他 mod 的附魔一律排除（数值强度、可附魔条件各不相同，容易出现异常）。
/// 可配置：剔除对玩家无实际收益的机制性附魔与测试用 mock/弃用附魔；带数值的附魔使用自定义强度。
/// </summary>
public static class EnchantHelper
{
    // 不参与随机附魔的附魔（原版机制性附魔，对玩家无实际收益）
    private static readonly HashSet<string> ExcludedEntries = new()
    {
        "clone",
        "goopy", // 战斗中会导致游戏卡死
        "imbued",
        "inky", // 战斗中会导致游戏卡死
        "slumbering_essence"
    };

    // 带数值附魔的强度；不在表中的附魔使用默认强度 1
    private static readonly Dictionary<string, decimal> StrengthByEntry = new()
    {
        ["adroit"] = 3m,
        ["nimble"] = 3m,
        ["sharp"] = 3m,
        ["swift"] = 3m,
        ["vigorous"] = 8m
    };

    /// <summary>给卡牌施加一个随机的可用原版附魔（多人同步：使用战斗生成 RNG）。</summary>
    public static void ApplyRandomEnchant(CardModel card, Player owner)
    {
        // 附魔的 Id.Entry 是大写（如 "ADROIT"），先归一化为小写再比较，
        // 否则剔除表和数值表永远匹配不上
        EnchantmentModel[] candidates = ModelDb.DebugEnchantments
            .Where(e => !e.IsMock
                && e is not DeprecatedEnchantment
                && IsFromVanillaOrThisMod(e)
                && !ExcludedEntries.Contains(e.Id.Entry.ToLowerInvariant())
                && e.CanEnchant(card))
            .ToArray();
        if (candidates.Length == 0)
        {
            return;
        }

        // 随机选一个附魔
        EnchantmentModel chosen = candidates
            .TakeRandom(1, owner.RunState.Rng.CombatCardGeneration)
            .First();

        // 带数值的附魔使用自定义强度，其余默认 1
        decimal amount = StrengthByEntry.TryGetValue(chosen.Id.Entry.ToLowerInvariant(), out decimal strength)
            ? strength
            : 1m;

        // canonical 附魔模型 → mutable 实例并施加
        CardCmd.Enchant(chosen.ToMutable(), card, amount);
    }

    // 只允许原版附魔与本 mod 附魔参与随机附魔：
    // 其他 mod 的附魔强度不明确、可附魔条件也可能与本 mod 的卡牌不兼容，排除后更安全
    private static bool IsFromVanillaOrThisMod(EnchantmentModel enchantment)
    {
        // 没有任何 mod 注册这个类型 → 原版附魔
        if (!ModContentRegistry.TryGetOwnerModId(enchantment.GetType(), out string ownerModId))
        {
            return true;
        }

        // 本 mod 注册的附魔 → 允许；其他 mod → 排除
        return string.Equals(ownerModId, Entry.ModId, StringComparison.OrdinalIgnoreCase);
    }
}
