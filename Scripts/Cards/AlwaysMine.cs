using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class AlwaysMine : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“奇巧”与“消耗”关键词（被弃置时改为免费打出，打出后因“消耗”进入消耗堆）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly, CardKeyword.Exhaust];

    // 卡牌基础数值：这张牌被消耗时从弃牌堆拿回手牌的张数（基础 2，升级后 3）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public AlwaysMine() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 被消耗时的效果逻辑（打出后因“消耗”进入消耗堆即触发；被弃置时经“奇巧”免费打出同样触发）
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        // 该钩子会广播给场上所有牌（含各牌堆），因此必须先确认被消耗的是自己
        if (card != this || Owner is null)
        {
            return;
        }

        // 播放音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/always_mine");

        // 从弃牌堆中选择至多 N 张牌放入手牌
        CardPile discard = PileType.Discard.GetPile(Owner);
        if (discard.Cards.Count == 0)
        {
            return;
        }

        int maxCount = DynamicVars.Cards.IntValue;
        if (discard.Cards.Count < maxCount)
        {
            maxCount = discard.Cards.Count;
        }

        // 上限同时受手牌空位约束：手牌满 10 张时多出的牌会被引擎静默转入弃牌堆（见 CardPileCmd.Add）
        int handSpace = CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count;
        if (handSpace < maxCount)
        {
            maxCount = handSpace;
        }

        if (maxCount <= 0)
        {
            return;
        }

        List<CardModel> selectedList = (await CardSelectCmd.FromCombatPile(
            context: choiceContext,
            pile: discard,
            player: Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_FROM_DISCARD"), 0, maxCount))).ToList();

        await CardPileCmd.Add(selectedList, PileType.Hand);
    }

    // 升级：从弃牌堆拿回的张数 2 → 3
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
