using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class GoldenRebellion : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public GoldenRebellion() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/golden_rebellion");

        // 从新三国卡池的稀有牌中生成 3 张候选（FilterForCombat 会自动排除不可战斗生成的牌），
        // 使用战斗生成 RNG 保证多人同步
        IEnumerable<CardModel> rareCards = ModelDb.CardPool<NewsanguoCardPool>().AllCards
            .Where(c => c.Rarity == CardRarity.Rare);
        List<CardModel> options = CardFactory.GetDistinctForCombat(base.Owner, rareCards, 3, base.Owner.RunState.Rng.CombatCardGeneration).ToList();

        // 升级后的“黄金起义”，三张候选均为升级版本
        if (IsUpgraded)
        {
            foreach (CardModel card in options)
            {
                CardCmd.Upgrade(card);
            }
        }

        // 从三张稀有牌中选择一张加入手牌，本回合内免费打出（与 Splash 一致）
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, base.Owner, canSkip: false);
        if (selected is null)
        {
            return;
        }

        selected.SetToFreeThisTurn();
        var result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, base.Owner);
        CardCmd.PreviewCardPileAdd(result, 1.2f);
    }
}
