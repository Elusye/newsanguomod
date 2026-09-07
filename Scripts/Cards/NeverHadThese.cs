using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class NeverHadThese : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 8（11）点格挡；每消耗一张非攻击牌获得 1 点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(8, ValueProp.Move),
        new PowerVar<HeavensForce>("heavens_force", 1)
    ];

    // 悬停提示：展示“格挡”、“天意之力”和“天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<HeavensForce>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 打出后能获得格挡
    public override bool GainsBlock => true;

    public NeverHadThese() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/never_had_these");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得 8（11）点格挡（先于消耗）
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, fast: false);

        // 收集手牌中的所有非攻击牌
        List<CardModel> nonAttackCards = PileType.Hand.GetPile(base.Owner).Cards
            .Where(card => card.Type != CardType.Attack)
            .ToList();
        if (nonAttackCards.Count == 0)
        {
            return;
        }

        // 每张获得 1 点天意之力
        int heavensForcePerCard = DynamicVars["heavens_force"].IntValue;

        // 消耗手牌中的所有非攻击牌
        foreach (CardModel card in nonAttackCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        await PowerCmd.Apply<HeavensForce>(
            choiceContext,
            base.Owner.Creature,
            heavensForcePerCard * nonAttackCards.Count,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级：格挡 8 → 11，并获得“保留”
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3);
        AddKeyword(CardKeyword.Retain);
    }

    // 降级：移除“保留”
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Retain);
    }
}
