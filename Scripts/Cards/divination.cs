using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class Divination : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 2 点天意之力，抽 2 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HeavensForcePower>("heavens_force", 2),
        new CardsVar(2)
    ];

    // 鼠标悬停时显示天意之力与天意侵蚀提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public Divination() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/divination");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得天意之力
        int heavensForceAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<HeavensForcePower>(
            choiceContext,
            base.Owner.Creature,
            heavensForceAmount,
            base.Owner.Creature,
            this,
            silent: false);

        // 抽两张牌
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, base.Owner);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 天意之力从 3 提高到 4
        DynamicVars["heavens_force"].UpgradeValueBy(1);
    }
}
