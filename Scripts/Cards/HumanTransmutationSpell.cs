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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HumanTransmutationSpell : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词（合并 base 以保留模板附加的模组关键词）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 无动态数值：不再消耗天意之力，金币消耗按实际变化张数直接结算（每张 1 金币）
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // 鼠标悬停时显示“士兵”卡牌标注（升级时显示升级版士兵）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<Soldier>(IsUpgraded)
    ];

    // 2026-10-04：不再消耗天意之力，按要求不计入天意牌（不再覆写 IsHeavensCard）；
    // “禁术牌”关键词已整体删除，因此也不再覆写 IsForbiddenSpell

    public HumanTransmutationSpell() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/human_transmutation_spell");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        CardPile hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count == 0)
        {
            return;
        }

        // 从手牌中选择任意张牌（最少 0 张，可不选）
        List<CardModel> selectedList = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_ANY"), 0, hand.Cards.Count),
            filter: null,
            source: this)).ToList();

        // 将选中的牌逐张变化为士兵（升级后为升级版的士兵）
        ICombatState combatState = base.CombatState!;

        // 成功变化的张数（变化失败时 CardCmd.Transform 返回 null，不计费）
        int changedCount = 0;

        foreach (CardModel original in selectedList)
        {
            CardModel soldierCard = combatState.CreateCard<Soldier>(base.Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(soldierCard);
            }
            CardPileAddResult? result = await CardCmd.Transform(original, soldierCard);
            if (result != null)
            {
                changedCount++;
            }
        }

        // 每变化一张牌失去 1 金币（金币不足时按剩余金币扣光）
        if (changedCount > 0)
        {
            await PlayerCmd.LoseGold(changedCount, base.Owner);
        }
    }
}
