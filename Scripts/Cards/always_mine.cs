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
public class always_mine : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：普通
    private const CardRarity rarity = CardRarity.Common;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“奇巧”关键词（打出时若正在弃牌可免费打出，不会真的消耗）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    // 卡牌基础数值：从弃牌堆拿回手牌的张数（基础 2，升级 3）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    public always_mine() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/always_mine");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 从弃牌堆中选择至多 N 张牌放入手牌
        CardPile discard = PileType.Discard.GetPile(owner);
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
            player: owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_FROM_DISCARD"), 0, maxCount))).ToList();

        foreach (CardModel card in selectedList)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 拿回手牌的张数从 2 提高到 3
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
