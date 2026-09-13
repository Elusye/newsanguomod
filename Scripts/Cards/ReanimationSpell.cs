using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
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
public class ReanimationSpell : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词（合并 base 以保留模板附加的模组关键词）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 卡牌基础数值：失去 5 点天意之力（变量用正值，打出时取负）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(5m)
    ];

    // 鼠标悬停时显示天意之力与天意侵蚀提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    // 禁术牌：牌名以“术”结尾
    public override bool IsForbiddenSpell => true;

    public ReanimationSpell() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/reanimation_spell");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 失去天意之力
        int lostAmount = DynamicVars["HeavensForcePower"].IntValue;
        await PowerCmd.Apply<HeavensForcePower>(
            choiceContext,
            base.Owner.Creature,
            -lostAmount,
            base.Owner.Creature,
            this,
            silent: false);

        // 从消耗牌堆中选择任意张牌放入手牌（不能选择消耗堆中另一张“亡灵复活术”，避免无限复活循环）
        CardPile exhaust = PileType.Exhaust.GetPile(base.Owner);
        if (exhaust.Cards.Count == 0)
        {
            return;
        }

        List<CardModel> selectedList = (await CardSelectCmd.FromCombatPile(
            context: choiceContext,
            pile: exhaust,
            player: base.Owner,
            prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_ANY"), 0, exhaust.Cards.Count),
            filter: card => card is not ReanimationSpell)).ToList();

        foreach (CardModel card in selectedList)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 失去的天意之力从 5 减少到 4
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
    }
}
