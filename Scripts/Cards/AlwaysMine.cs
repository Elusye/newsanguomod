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

    // 卡牌自带“奇巧”与“消耗”关键词（打出时若正在弃牌可免费打出，不会真的消耗）
    // “消耗”在升级后移除（见 OnUpgrade），因此只能用关键词动态增删
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly, CardKeyword.Exhaust];

    // 卡牌基础数值：从弃牌堆拿回手牌的张数（固定 3，升级不改变张数）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];

    public AlwaysMine() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/always_mine");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 从弃牌堆中选择至多 N 张牌放入手牌
        CardPile discard = PileType.Discard.GetPile(base.Owner);
        if (discard.Cards.Count == 0)
        {
            return;
        }

        int maxCount = (int)DynamicVars.Cards.BaseValue;
        if (discard.Cards.Count < maxCount)
        {
            maxCount = discard.Cards.Count;
        }

        List<CardModel> selectedList = (await CardSelectCmd.FromCombatPile(
            context: choiceContext,
            pile: discard,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_FROM_DISCARD"), 0, maxCount))).ToList();

        foreach (CardModel card in selectedList)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 升级后不再“消耗”
        RemoveKeyword(CardKeyword.Exhaust);
    }

    // 降级回退：恢复“消耗”
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Exhaust);
    }
}
