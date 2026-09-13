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
public class MyThreeGenerals : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 鼠标悬停时展示三张可选将领（升级时展示对应升级版）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<HanXin>(IsUpgraded),
        HoverTipFactory.FromCard<BaiQi>(IsUpgraded),
        HoverTipFactory.FromCard<ZhouYafu>(IsUpgraded)
    ];

    public MyThreeGenerals() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/my_three_generals");

        // 1. 选择并消耗一张手牌（手牌为空时跳过，仍可执行选择）
        var hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count > 0)
        {
            CardModel? cardToExhaust = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: base.Owner,
                prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_ONE_TO_EXHAUST"), 1, 1),
                filter: null,
                source: this)).FirstOrDefault();
            if (cardToExhaust is not null)
            {
                await CardCmd.Exhaust(choiceContext, cardToExhaust);
            }
        }

        // 2. 生成韩信、白起、周亚夫三张候选（升级后均为升级版）
        ICombatState combatState = base.CombatState!;

        List<CardModel> options =
        [
            combatState.CreateCard<HanXin>(base.Owner),
            combatState.CreateCard<BaiQi>(base.Owner),
            combatState.CreateCard<ZhouYafu>(base.Owner)
        ];
        if (IsUpgraded)
        {
            foreach (CardModel general in options)
            {
                CardCmd.Upgrade(general);
            }
        }

        // 3. 三选一加入手牌
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, base.Owner, canSkip: false);
        if (selected is null)
        {
            return;
        }

        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, base.Owner, CardPilePosition.Random);
    }

    // 升级：加入的三张候选变为升级版（由 IsUpgraded 在打出时判断）
    protected override void OnUpgrade()
    {
    }
}
