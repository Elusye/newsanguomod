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

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

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

        // 从三张稀有牌中选择一张加入手牌，本回合内免费打出（与 Splash 一致）。
        // canSkip: true —— 选择界面上会多出一个「跳过」按钮（原版 Splash 同款写法，Splash.cs:41
        // 用的就是 canSkip: true）；跳过后返回 null，这里直接结束。
        // 多人下「跳过」由 PlayerChoiceSynchronizer 以 -1 索引同步（CardSelectCmd.cs:247-256），
        // 远端同样得到 null，不会出现两端分歧。
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, base.Owner, canSkip: true);
        if (selected is null)
        {
            return;
        }

        selected.SetToFreeThisTurn();
        var result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, base.Owner);
        CardCmd.PreviewCardPileAdd(result, 1.2f);
    }
}
