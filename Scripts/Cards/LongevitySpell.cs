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
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
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

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡牌基础数值：失去 5 点天意之力（升级后 4）
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ("ForceLoss", 5m)];

    // 鼠标悬停时显示天意之力、灵魂附魔与消耗关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        ..HoverTipFactory.FromEnchantment<SoulsPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

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
        await PowerCmd.Apply<HeavensForcePower>(
            choiceContext,
            base.Owner.Creature,
            -DynamicVars["ForceLoss"].BaseValue,
            base.Owner.Creature,
            this);

        // 选择一张手牌中无附魔、自带“消耗”关键词的牌，附加“灵魂”附魔（移除其消耗）
        CardModel? selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext,
            player: base.Owner,
            filter: card => card.Enchantment is null && card.Keywords.Contains(CardKeyword.Exhaust),
            source: this)).FirstOrDefault();
        if (selected is not null)
        {
            CardCmd.Enchant<SoulsPower>(selected, 1m);
        }
    }

    // 升级后的效果逻辑：失去的天意之力 5 → 4，并获得“保留”
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
        DynamicVars["ForceLoss"].UpgradeValueBy(-1m);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Retain);
    }
}
