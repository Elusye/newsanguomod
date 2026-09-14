using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “你拾它做甚！”留下的能力：在你的下个回合开始时（抽牌前），从自己的弃牌堆中选择至多 Amount 张牌加入手牌，
/// 随后移除本能力。
///
/// 选择时机之所以放在回合开始而不是打出瞬间：原实现在打出时立即开启选择界面，
/// 若该牌在“额外回合”中被打出，战斗流程会卡死（回合无法结束）。
/// 本能力仿照原版“既定结局”（<c>ForegoneConclusionPower</c>，从抽牌堆取牌）实现，区别在于选取的是弃牌堆。
/// </summary>
[RegisterPower]
public class WhyPickThatUpPower : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 计数器：图标右下角显示可选张数上限（Amount）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 选择界面提示语：沿用卡牌已有的本地化键，避免新增键在未重新导出的 pck 中缺失而抛异常
    private static LocString Prompt => new("cards", "NEWSANGUO_CARD_SELECT_FROM_DISCARD");

    // 玩家回合开始（抽牌前）时，从该玩家自己的弃牌堆中选牌加入其手牌
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        // 钩子对所有玩家逐个触发，此处只处理本能力的宿主玩家
        if (player != Owner.Player)
        {
            return;
        }

        CardPile discard = PileType.Discard.GetPile(player);
        if (discard.Cards.Count > 0)
        {
            int maxCount = Math.Min((int)Amount, discard.Cards.Count);

            // 触发“你拾它作甚！”能力音效（对应 FMOD 事件 event:/newsanguo/sfx/why_pick_that_up_power）
            NewsanguoSfx.Play("event:/newsanguo/sfx/why_pick_that_up_power");

            foreach (CardModel card in await CardSelectCmd.FromCombatPile(
                context: choiceContext,
                pile: discard,
                player: player,
                prefs: new CardSelectorPrefs(Prompt, 0, maxCount)))
            {
                // 牌属于该玩家，按牌主解析对应手牌堆
                await CardPileCmd.Add(card, PileType.Hand);
            }
        }

        await PowerCmd.Remove(this);
    }
}
