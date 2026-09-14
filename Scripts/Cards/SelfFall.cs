using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class SelfFall : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每打出一张牌失去的生命
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("HpCostPerCard", 1m)
    ];

    public SelfFall() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/self_fall");

        // 快照抽牌堆中的所有攻击牌（避免移动过程中集合变化）
        CardPile drawPile = PileType.Draw.GetPile(base.Owner);
        List<CardModel> attackCards = drawPile.Cards
            .Where(c => c.Type == CardType.Attack && !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList();

        // 将抽牌堆中的所有攻击牌放入手牌，并设为本回合内免费打出（同“发现”）
        // 注意：必须先设置免费再入牌堆——与原版 MadScience/Discovery 一致，否则打出后费用无法正常重置
        foreach (CardModel card in attackCards)
        {
            card.SetToFreeThisTurn();
            await CardPileCmd.Add(card, PileType.Hand);
        }

        // 附加“自刎”能力：本回合内每打出一张攻击牌，对自己造成 HpCostPerCard 点伤害（仅持续本回合）。
        // 同一回合多次打出会叠加能力 Amount（如两张为每张 2 点），但不会延长持续时间；
        // 能力内部会在“打出前登记、打出后核销”，因此附加它的这张牌本身不会触发。
        await PowerCmd.Apply<BloodLossPower>(choiceContext, base.Owner.Creature, DynamicVars["HpCostPerCard"].IntValue, base.Owner.Creature, this);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 升级后获得“保留”关键词
        AddKeyword(CardKeyword.Retain);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Retain);
    }
}
