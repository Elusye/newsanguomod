using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class lets_discuss : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词（升级后移除）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public lets_discuss() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        NewsanguoSfx.Play("event:/newsanguo/sfx/lets_discuss");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 抽牌直到抽满手牌（Draw 内部会按手牌上限自动截断）
        List<CardModel> drawn = (await CardPileCmd.Draw(choiceContext, 999, owner)).ToList();
        if (drawn.Count == 0)
        {
            return;
        }

        // 由玩家选择丢弃等量的牌
        List<CardModel> toDiscard = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: owner,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, drawn.Count, drawn.Count),
            filter: null,
            source: this)).ToList();
        if (toDiscard.Count > 0)
        {
            await CardCmd.Discard(choiceContext, toDiscard);
        }
    }

    // 升级后的效果逻辑：去除“消耗”
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）：加回“消耗”
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Exhaust);
    }
}
