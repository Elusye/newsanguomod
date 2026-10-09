using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// “募集”的悬停提示工厂（青州兵机制的关键词说明）。
///
/// 为什么不是 RitsuLib 的模组关键词：模组关键词（<c>[RegisterOwnedCardKeyword]</c>）的定义
/// （STS2RitsuLib.Keywords.ModKeywordDefinition）只有固定的 title/description 本地化键，
/// 没有任何 DynamicVar 参数化设施，且提示创建时（HoverTipFactoryFromKeywordPatch）拿不到卡牌上下文
/// ⇒ 标题里的数量永远是静态文案，无法显示“募集1”这样的卡面数值。
///
/// 原版“召唤{Summon}”走的是另一条路（sts2.decompiled.cs:164298-164309 的 HoverTipFactory.Static）：
/// 自己 new LocString + <c>LocString.Add(DynamicVar)</c> 再 new HoverTip(...) —— HoverTip 的构造函数
/// 会对 LocString 立即求值（:164059-164070），所以变量必须在构造前塞进去。本类照抄这条路，
/// 于是标题/说明里的 {Summon} 会按传入的数值渲染（卡牌传卡面的 Summon，能力传当前层数）。
///
/// 文案表：模组只能合并进原版已有的本地化表（见 Scripts/Combat/HeavensForce.cs:37-39），
/// 故放在 static_hover_tips 表（newsanguo/localization/{zhs,eng}/static_hover_tips.json）。
///
/// 2026-10-09 需求（用户 m00512 → m00717 修订）：只有“募集”一个关键词，标题为「募集{数量}」；
/// 说明里带“在你的回合开始时，失去所有募集的青州兵。”（本 mod 的青州兵都在回合开始时失去，
/// 所以两套说明合并成一条，不再有“临时募集”）。
/// </summary>
public static class RecruitKeyword
{
    private const string LocTable = "static_hover_tips";

    // 提示里数量所用的变量名：与文案中的 {Summon} 对应（原版 SummonVar 的名字，见 sts2.decompiled.cs:163788）
    private const string AmountVarName = "Summon";

    private const string RecruitTitleKey = "NEWSANGUO_KEYWORD_RECRUIT.title";
    private const string RecruitDescriptionKey = "NEWSANGUO_KEYWORD_RECRUIT.description";

    /// <summary>
    /// “募集”提示：标题为「募集{数量}」，说明为“生成{数量}名1生命值的青州兵。……”
    /// （含“在你的回合开始时，失去所有募集的青州兵。”）；
    /// 数量使用卡面自己的变量（如曹氏兵法的 <c>DynamicVars.Summon</c>）。
    /// </summary>
    public static IHoverTip Recruit(DynamicVar amount)
    {
        LocString title = new LocString(LocTable, RecruitTitleKey);
        LocString description = new LocString(LocTable, RecruitDescriptionKey);

        // 变量必须在 new HoverTip(...) 之前塞进去：HoverTip 的构造函数会立刻求值（sts2.decompiled.cs:164059-164070）
        title.Add(amount);
        description.Add(amount);

        return new HoverTip(title, description);
    }

    /// <summary>“募集”提示，直接给数量（用于没有同名 DynamicVar 的场合，如能力层数）。</summary>
    public static IHoverTip Recruit(decimal amount)
    {
        return Recruit(new DynamicVar(AmountVarName, amount));
    }
}
