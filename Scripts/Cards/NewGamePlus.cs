using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
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
public class NewGamePlus : NewsanguoCardTemplate
{

    // 带“预见”关键词（悬停显示预见机制说明）
    protected override bool IsScryCard => true;

    // 卡牌自带“消耗”关键词（合并 base 以保留“预见”等模板附加的模组关键词）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：预见数量、天意之力、酒力、抽牌数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("ScryAmount", 5m),
        new PowerVar<HeavensForcePower>(2m),
        new PowerVar<DrunkenMightPower>(3m),
        new CardsVar(1)
    ];

    // 鼠标悬停时显示天意之力、天意侵蚀与酒力提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>(),
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public NewGamePlus() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/new_game_plus");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 预见（查看抽牌堆顶部 N 张，可选择丢弃任意张）
        await ScryCmd.Scry(choiceContext, base.Owner, DynamicVars["ScryAmount"].IntValue);

        // 获得天意之力
        await PowerCmd.Apply<HeavensForcePower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["HeavensForcePower"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);

        // 获得酒力
        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["DrunkenMightPower"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);

        // 抽 1 张牌
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, base.Owner);
    }

    // 升级：获得“固有”
    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    // 降级：移除“固有”
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Innate);
    }
}
