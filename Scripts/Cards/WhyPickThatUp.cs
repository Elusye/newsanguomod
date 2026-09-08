using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WhyPickThatUp : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 自带“奇巧”（Sly）与“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly, CardKeyword.Exhaust];

    // 每名玩家最多可从弃牌堆拿回手牌的张数（升级不变，费用 3 → 2）
    private const int MaxCardsPerPlayer = 10;

    public WhyPickThatUp() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/why_pick_that_up");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        ICombatState combatState = CombatState!;

        // 所有存活的玩家每人各选至多 10 张自己弃牌堆中的牌加入自己的手牌
        // 打出者优先，其余队友随后（顺序进行选择）
        IEnumerable<Player> targets = combatState.Players
            .Where(p => p.Creature is { IsAlive: true })
            .OrderByDescending(p => p == Owner);

        foreach (Player player in targets)
        {
            CardPile discard = PileType.Discard.GetPile(player);
            if (discard.Cards.Count == 0)
            {
                continue;
            }

            int maxCount = discard.Cards.Count < MaxCardsPerPlayer ? discard.Cards.Count : MaxCardsPerPlayer;

            List<CardModel> selected = (await CardSelectCmd.FromCombatPile(
                context: choiceContext,
                pile: discard,
                player: player,
                prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_FROM_DISCARD"), 0, maxCount))).ToList();

            foreach (CardModel card in selected)
            {
                // 牌属于该玩家，按牌主解析对应手牌堆
                await CardPileCmd.Add(card, PileType.Hand);
            }
        }
    }

    // 升级：费用 3 → 2
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
