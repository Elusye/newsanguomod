using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using newsanguo.Scripts.Cards;
namespace newsanguo.Scripts.Patches;

// 旧版牌归属衍生池，但保留普通稀有度；补入图鉴“其他”分类。
[HarmonyPatch(typeof(NCardLibrary), "_Ready")]
public static class OldCardLibraryPatch
{
    public static void Postfix(NCardPoolFilter ____miscPoolFilter,
        Dictionary<NCardPoolFilter, Func<CardModel, bool>> ____poolFilters)
    {
        if (!____poolFilters.TryGetValue(____miscPoolFilter, out var original)) return;
        ____poolFilters[____miscPoolFilter] = card => original(card) || card is
            OldForgottenRitual or OldBorrowedTime or OldDirge or OldCompact or
            OldExpectAFight or OldFuel or OldMementoMori or OldMythicForm or
            OldTheHierophant or OldTheEmperor or OldTheTower or OldTemperance or OldTheSun or OldDeath;
    }
}
