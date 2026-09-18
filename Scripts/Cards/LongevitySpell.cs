using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class LongevitySpell : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词（合并 base 以保留模板附加的模组关键词）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 卡牌基础数值：失去 5 点天意之力（升级后 4）；抽 3 张牌（升级后 5）
    // HeavensForceVar：被“魔法禁术目录”标记的回合内，卡面显示 0 点
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(5m),
        new CardsVar(3)
    ];

    // 鼠标悬停时显示天意之力、天意侵蚀、灵魂附魔与消耗关键词说明
    // （天意之力的说明文本中会出现“天意侵蚀”，两者须成对展示）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>(),
        ..HoverTipFactory.FromEnchantment<SoulsPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    // 禁术牌：牌名以“术”结尾
    public override bool IsForbiddenSpell => true;

    public LongevitySpell() :
        base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（FMOD 事件由 mod 维护者自行创建）
        NewsanguoSfx.Play("event:/newsanguo/sfx/longevity_spell");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 失去天意之力
        await HeavensForce.Add(choiceContext, base.Owner, -DynamicVars["HeavensForcePower"].IntValue, this);

        // 失去天意之力后抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, base.Owner);

        // 选择任意张手牌中无附魔、自带“消耗”关键词的牌，附加“灵魂”附魔（移除其消耗）
        List<CardModel> selectableCards = PileType.Hand.GetPile(base.Owner).Cards
            .Where(card => card.Enchantment is null && card.Keywords.Contains(CardKeyword.Exhaust))
            .ToList();
        if (selectableCards.Count == 0)
        {
            return;
        }

        List<CardModel> selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 0, selectableCards.Count),
            context: choiceContext,
            player: base.Owner,
            filter: selectableCards.Contains,
            source: this)).ToList();
        foreach (CardModel card in selected)
        {
            CardCmd.Enchant<SoulsPower>(card, 1m);
        }
    }

    // 升级后的效果逻辑：失去的天意之力 5 → 4、抽牌数 3 → 5
    protected override void OnUpgrade()
    {
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1m);
        DynamicVars["Cards"].UpgradeValueBy(2m);
    }
}
