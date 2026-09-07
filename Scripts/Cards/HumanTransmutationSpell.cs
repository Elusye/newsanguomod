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

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HumanTransmutationSpell : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡牌基础数值：失去 5 点天意之力（变量用正值，打出时取负）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HeavensForce>("heavens_force", 5)
    ];

    // 鼠标悬停时显示“士兵”卡牌标注（升级时显示升级版士兵）、天意之力与天意侵蚀提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<Soldier>(IsUpgraded),
        HoverTipFactory.FromPower<HeavensForce>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

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

        // 失去天意之力
        int lostAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<HeavensForce>(
            choiceContext,
            base.Owner.Creature,
            -lostAmount,
            base.Owner.Creature,
            this,
            silent: false);

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

        foreach (CardModel original in selectedList)
        {
            CardModel soldierCard = combatState.CreateCard<Soldier>(base.Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(soldierCard);
            }
            await CardCmd.Transform(original, soldierCard);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 失去的天意之力从 5 减少到 4
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
    }
}
