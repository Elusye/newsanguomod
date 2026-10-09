using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Helpers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class SeaChange : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：选择至多 3 张手牌变化（升级后 5 张）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];

    // 悬停提示：本牌会给天意之力，补一条说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip()
    ];

    // 属于“天意”体系（涉及天意之力）
    public override bool IsHeavensCard => true;

    public SeaChange() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/sea_change");

        CardPile hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count == 0)
        {
            return;
        }

        // 从手牌中选择至多 Cards 张（最少 0 张，可以不选）；可选上限同时受手牌数限制
        int maxCount = Math.Min(DynamicVars["Cards"].IntValue, hand.Cards.Count);
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 0, maxCount),
            filter: null,
            source: this)).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        // 逐张随机变化，并为变化出来的牌添加随机附魔
        // （选择结果已快照成列表，避免变换过程中集合变化）
        // 同时统计真正变化成功的张数，用于结算天意之力
        Rng rng = base.Owner.RunState.Rng.CombatCardSelection;
        int changedCount = 0;
        foreach (CardModel original in selected)
        {
            CardPileAddResult result = await CardCmd.TransformToRandom(original, rng);
            if (result.cardAdded != null)
            {
                EnchantHelper.ApplyRandomEnchant(result.cardAdded, base.Owner);
                changedCount++;
            }
        }

        // 追加：每变化一张牌，获得等量（1 点/张）天意之力（2026-10-01 追加）
        if (changedCount > 0)
        {
            await HeavensForce.Add(choiceContext, base.Owner, changedCount, this);
        }
    }

    // 升级后的效果逻辑：可选张数从 3 提高到 5
    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(2m);
    }
}
