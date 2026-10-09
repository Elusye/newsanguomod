using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;


namespace newsanguo.Scripts.Cards;

/// <summary>
/// 我不能走啊！（诅咒）：99 费 + 奇巧 + 固有 + 保留 + 消耗 + 永恒。
/// 被消耗时，将一张此牌的复制品加入手牌 —— 消耗不掉，越消耗越多。
/// </summary>
// 注册到诅咒卡池（与其他诅咒牌一起，可供诅咒奖励/事件获取）
[RegisterCard(typeof(CurseCardPool))]
public class ICantLeave : NewsanguoCurseTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 五条关键词的文本由引擎按 CardKeywordOrder 自动追加，不需要写进描述：
    // 奇巧 / 保留 / 固有 排在描述之前，消耗 / 永恒 排在描述之后（CardKeywordOrder.cs）。
    //  · 奇巧（Sly）：这张牌被丢弃时会被免费自动打出（CardCmd.cs:201-204 对所有被丢弃的
    //    Sly 牌调用 AutoPlay，AutoPlay 不结算能量消耗，CardCmd.cs:123-130），
    //    打出后因带“消耗”进消耗牌堆，再触发下面的复制效果 —— 丢也丢不掉，与牌名呼应。
    //  · 永恒（Eternal）：不能被移出牌组（CardModel.IsRemovable），但不阻止被消耗。
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Sly,
        CardKeyword.Innate,
        CardKeyword.Retain,
        CardKeyword.Exhaust,
        CardKeyword.Eternal
    ];

    // 99 费：正常对局几乎不可能打出（不再是“不能被打出”关键词，卡面会显示 99 费）
    public ICantLeave() : base(99)
    {
    }

    // 这张牌被消耗时：把一张复制品加入手牌。
    // 时机：CardCmd.Exhaust 会先把牌放进消耗牌堆、再派发 AfterCardExhausted（CardCmd.cs:242-244），
    // 所以此刻 this 仍在战斗牌堆里，CreateClone 可用（它要求源牌位于战斗牌堆）。
    // 用克隆而不是新建一张同名牌：克隆是战斗内的复制品（不会进牌组），并完整继承当前关键词；
    // 复制品再次被消耗会再复制一张 —— 这正是这张诅咒的设计意图。
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this)
        {
            return;
        }

        // 播放音效（资源文件 i_cant_leave，响度补偿见 NewsanguoSfx.LoudnessGainDb）
        NewsanguoSfx.Play("event:/newsanguo/sfx/i_cant_leave");

        CardModel copy = CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
    }
}
