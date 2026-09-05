using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
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
public class my_three_generals : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 鼠标悬停时展示三张可选将领（升级时展示对应升级版）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<han_xin>(IsUpgraded),
        HoverTipFactory.FromCard<bai_qi>(IsUpgraded),
        HoverTipFactory.FromCard<zhou_yafu>(IsUpgraded)
    ];

    public my_three_generals() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        NewsanguoSfx.Play("event:/newsanguo/sfx/my_three_generals");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 1. 选择并消耗一张手牌（手牌为空时跳过，仍可执行选择）
        var hand = PileType.Hand.GetPile(owner);
        if (hand.Cards.Count > 0)
        {
            CardModel? cardToExhaust = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: owner,
                prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_ONE_TO_EXHAUST"), 1, 1),
                filter: null,
                source: this)).FirstOrDefault();
            if (cardToExhaust is not null)
            {
                await CardCmd.Exhaust(choiceContext, cardToExhaust);
            }
        }

        // 2. 生成韩信、白起、周亚夫三张候选（升级后均为升级版）
        ICombatState? combatState = base.CombatState;
        if (combatState is null)
        {
            return;
        }

        List<CardModel> options =
        [
            combatState.CreateCard<han_xin>(owner),
            combatState.CreateCard<bai_qi>(owner),
            combatState.CreateCard<zhou_yafu>(owner)
        ];
        if (IsUpgraded)
        {
            foreach (CardModel general in options)
            {
                CardCmd.Upgrade(general);
            }
        }

        // 3. 三选一加入手牌
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, owner, canSkip: false);
        if (selected is null)
        {
            return;
        }

        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, owner, CardPilePosition.Random);
    }

    // 升级：加入的三张候选变为升级版（由 IsUpgraded 在打出时判断）
    protected override void OnUpgrade()
    {
    }
}
