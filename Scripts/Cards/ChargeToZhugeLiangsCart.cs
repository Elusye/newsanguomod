using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池（曹魏新增）
// 「直奔诸葛亮四轮车！」：把抽牌堆＋弃牌堆里所有未消耗的攻击牌抓进手牌，
// 本回合内攻击牌免费打出，且用攻击牌击杀敌人时获得衍生牌「四轮车」。
[RegisterCard(typeof(NewsanguoCardPool))]
public class ChargeToZhugeLiangsCart : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 词条：消耗（升级前、升级后都有，升级只减费）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 悬停时展示衍生牌「四轮车」（不随升级变化：四轮车没有升级版）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<FourWheeledCart>()
    ];

    public ChargeToZhugeLiangsCart() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 升级效果：费用 3 → 2（其余效果不变）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/charge_to_zhuge_liangs_cart");

        // 手牌上限 10：超出部分会被引擎静默转入弃牌堆（见 CardPileCmd.Add），所以先按空位截断
        int handSpace = CardPile.MaxCardsInHand - PileType.Hand.GetPile(base.Owner).Cards.Count;
        List<CardModel> attackCards = [];
        if (handSpace > 0)
        {
            // 只抓“未消耗”的攻击牌：抽牌堆 + 弃牌堆（消耗堆既不取也不该取）
            foreach (PileType pileType in new[] { PileType.Draw, PileType.Discard })
            {
                attackCards.AddRange(
                    pileType.GetPile(base.Owner).Cards.Where(card => card.Type == CardType.Attack));
            }

            if (attackCards.Count > handSpace)
            {
                attackCards = attackCards.GetRange(0, handSpace);
            }
        }

        if (attackCards.Count > 0)
        {
            await CardPileCmd.Add(attackCards, PileType.Hand);
        }

        // 本回合内：攻击牌免费打出 + 攻击牌击杀敌人时给四轮车（回合结束时由能力自己移除）
        await PowerCmd.Apply<ChargeToZhugeLiangsCartPower>(
            choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);
    }
}
