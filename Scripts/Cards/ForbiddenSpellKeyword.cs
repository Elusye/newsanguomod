using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 注册“禁术牌”关键词。牌名以“术”结尾的卡牌（IsForbiddenSpell）会附加该关键词：
/// 卡牌描述开头自动插入“禁术牌。”标题，悬停卡牌时显示 card_keywords.json 中的说明。
/// 目前包含：人体炼成术、将领延寿术、心灵控制术、亡灵复活术。
/// </summary>
[RegisterOwnedCardKeyword("forbidden_spell",
    CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
public sealed class ForbiddenSpellKeyword
{
}
