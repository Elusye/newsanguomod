namespace newsanguo.Scripts.Api;

/// <summary>
/// newsanguo 的 Token 卡种类，供 <see cref="NewsanguoPublicApi.AddTokenToHand"/> 使用。
/// 枚举名与成员是跨 Mod 契约，发布后不要改名（新增请追加在末尾）。
/// </summary>
public enum NewsanguoTokenKind
{
    /// <summary>军杖：0 费攻击，打出后消耗。</summary>
    MilitaryCudgel = 0,

    /// <summary>士兵：0 费攻击，打出后消耗（「天降雄兵」「人体炼成术」等生成）。</summary>
    Soldier = 1,

    /// <summary>不胜酒力：状态牌，在手牌时本方无法获得酒力。</summary>
    Lightweight = 2,
}
