using STS2RitsuLib.Interop.AutoRegistration;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 注册“预见”关键词。带预见的卡牌（IsScryCard）会附加该关键词，
/// 悬停卡牌时显示预见机制的说明（card_keywords.json 中的描述）。
/// 具体预见数量写在卡牌描述中（如“预见7。”），不走关键词参数。
/// </summary>
[RegisterOwnedCardKeyword("scry")]
public sealed class ScryKeyword
{
}
