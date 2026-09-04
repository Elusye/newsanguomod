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
public class self_fall : NewsanguoCardTemplate
{
    // 基础耗能：3
    private const int energyCost = 3;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：稀有
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每打出一张牌失去的生命
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("HpCostPerCard", 3m)
    ];

    public self_fall() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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
        NewsanguoSfx.Play("event:/newsanguo/sfx/self_fall");

        // 快照抽牌堆中的所有攻击牌（避免移动过程中集合变化）
        CardPile drawPile = PileType.Draw.GetPile(owner);
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

        // 附加“自刎”能力：本回合内每打出一张攻击牌，失去 HpCostPerCard 点生命（仅持续本回合）。
        // 同一回合多次打出会叠加每次掉血数值（如两张为每牌失去 6 点），但不会延长持续时间；
        // 记录来源卡牌，保证打出“自刎归天”本身不掉血。
        int hpCost = DynamicVars["HpCostPerCard"].IntValue;
        blood_loss? power = owner.Creature.GetPowerInstances<blood_loss>().FirstOrDefault();
        if (power is null)
        {
            power = await PowerCmd.Apply<blood_loss>(choiceContext, owner.Creature, 1, owner.Creature, this);
        }
        power?.AddHpCost(hpCost);
        power?.SetSourceCard(this);
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
