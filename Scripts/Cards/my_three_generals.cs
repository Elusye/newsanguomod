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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
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

    // 鼠标悬停时展示加入手牌的韩信、白起、周亚夫（升级时展示对应升级版）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<han_xin>(IsUpgraded),
        HoverTipFactory.FromCard<bai_qi>(IsUpgraded),
        HoverTipFactory.FromCard<zhou_yafu>(IsUpgraded)
    ];

    // 卡牌基础数值：消耗 3 张手牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("exhaust_count", 3)
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

        SfxCmd.Play("event:/newsanguo/sfx/my_three_generals");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 1. 选择并消耗三张手牌（手牌不足三张时自动全选）
        int exhaustCount = DynamicVars["exhaust_count"].IntValue;
        CardModel[] cardsToExhaust = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: owner,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, exhaustCount, exhaustCount),
            filter: null,
            source: this)).ToArray();

        foreach (CardModel card in cardsToExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 2. 将韩信、白起、周亚夫各一张加入你的手牌（升级后为升级版）
        ICombatState? combatState = base.CombatState;
        if (combatState is null)
        {
            return;
        }

        foreach (CardModel general in new CardModel[]
        {
            combatState.CreateCard<han_xin>(owner),
            combatState.CreateCard<bai_qi>(owner),
            combatState.CreateCard<zhou_yafu>(owner),
        })
        {
            if (IsUpgraded)
            {
                CardCmd.Upgrade(general);
            }

            await CardPileCmd.AddGeneratedCardToCombat(general, PileType.Hand, owner, CardPilePosition.Random);
        }
    }

    // 升级：加入的衍生牌变为升级版（由 IsUpgraded 在打出时判断）
    protected override void OnUpgrade()
    {
    }
}
