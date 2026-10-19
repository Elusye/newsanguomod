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
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class AlwaysMine : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public override bool IsEmberCard => true;
    public override bool IsHeavensCard => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly, .. base.CanonicalKeywords];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new HeavensForceVar(2m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(), HoverTipFactory.FromPower<HeavensDecayPower>()
    ];
    private bool _resolvingEmber;

    public AlwaysMine() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this || _resolvingEmber)
            return;
        _resolvingEmber = true;
        try
        {
            // 打出本体，结算后按正常规则进入弃牌堆；不生成复制品。
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
        finally
        {
            _resolvingEmber = false;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/always_mine");
        await HeavensForce.Gain(choiceContext, Owner, DynamicVars["HeavensForcePower"].IntValue, this);

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

    // 升级：从弃牌堆拿回的张数 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
