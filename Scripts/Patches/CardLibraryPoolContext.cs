using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace newsanguo.Scripts.Patches;

/// <summary>
/// 卡牌图鉴里展示的是不可变的"规范牌"（没有 Owner、不属于任何卡组），
/// 所以 <c>NewsanguoCardTemplate.VisualCardPool</c> 没法从角色推断卡池，只能拿到
/// “ModelDb 里第一个包含该牌 id 的卡池”。我们的共享牌同时登记在新三国池与蜀汉池里，
/// 于是图鉴里永远是先命中的新三国池 → 蜀汉页的牌也是棕色。
/// 这里在玩家点击图鉴顶部的卡池筛选按钮时，把这个按钮对应的卡池记下来，
/// 让图鉴里的共享牌按“当前正在浏览的卡池”上色（蜀汉 = 墨绿，曹魏 = 棕色）。
/// 依赖 RitsuLib 给筛选节点起的名字：<c>MOD_FILTER_{CharacterModel.Id.Entry}</c>；
/// 原版角色的按钮（Ironclad/Silent…）不是这个前缀，解析不到就保持 null，
/// 颜色回退到原版逻辑，不会把别的角色的牌染错。
/// </summary>
internal static class CardLibraryPoolContext
{
    private const string FilterNamePrefix = "MOD_FILTER_";

    /// <summary>当前图鉴正在浏览的卡池；null 表示未知（按原版逻辑上色）。</summary>
    internal static CardPoolModel? CurrentPool { get; private set; }

    internal static void OnPoolFilterUpdated(NCardPoolFilter? filter)
    {
        CurrentPool = ResolvePool(filter);
    }

    /// <summary>图鉴界面重新打开时清空上次残留的上下文（_Ready 之前调用）。</summary>
    internal static void Reset()
    {
        CurrentPool = null;
    }

    private static CardPoolModel? ResolvePool(NCardPoolFilter? filter)
    {
        if (filter == null)
        {
            return null;
        }

        string name = filter.Name.ToString();
        if (!name.StartsWith(FilterNamePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        string entry = name.Substring(FilterNamePrefix.Length);
        if (entry.Length == 0)
        {
            return null;
        }

        foreach (CharacterModel character in ModelDb.AllCharacters)
        {
            if (string.Equals(character.Id.Entry, entry, StringComparison.OrdinalIgnoreCase))
            {
                return character.CardPool;
            }
        }

        return null;
    }
}

/// <summary>
/// 图鉴里的卡池筛选按钮是单选按钮（只能切到别的池，不能取消选中），
/// 所以 <c>NCardLibrary.UpdateCardPoolFilter</c> 每次被调用都意味着“玩家切到了这个池”。
/// 必须用 Prefix：原版方法体会在内部触发卡牌重绘，放到 Postfix 就晚了。
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), "UpdateCardPoolFilter")]
internal static class CardLibraryPoolFilterPatch
{
    [HarmonyPrefix]
    private static void Prefix(NCardPoolFilter filter)
    {
        try
        {
            CardLibraryPoolContext.OnPoolFilterUpdated(filter);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[CardLibrary] 解析图鉴卡池筛选失败：{e.Message}", 1);
        }
    }
}

/// <summary>
/// 图鉴界面重新打开时先清掉上次残留的“当前卡池”（用 Prefix：原版 <c>_Ready</c> 内部会选中默认筛选按钮，
/// 那时 Prefix 已经把上下文清空，随后默认按钮自己的 UpdateCardPoolFilter 会把正确的池填回去）。
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
internal static class CardLibraryPoolResetPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        try
        {
            CardLibraryPoolContext.Reset();
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"[CardLibrary] 重置图鉴卡池上下文失败：{e.Message}", 1);
        }
    }
}
