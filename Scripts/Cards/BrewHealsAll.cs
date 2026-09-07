using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
public class BrewHealsAll : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：5 点酒力、3 点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMight>("drunken_might", 5),
        new PowerVar<HeavensForce>("heavens_force", 3)
    ];

    // 悬停提示：展示“酒力”、“天意之力”、“天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMight>(),
        HoverTipFactory.FromPower<HeavensForce>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    public BrewHealsAll() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/brew_heals_all");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得酒力与天意之力
        int drunkenMightAmount = DynamicVars["drunken_might"].IntValue;
        int heavensForceAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<DrunkenMight>(choiceContext, base.Owner.Creature, drunkenMightAmount, base.Owner.Creature, this, silent: false);
        await PowerCmd.Apply<HeavensForce>(choiceContext, base.Owner.Creature, heavensForceAmount, base.Owner.Creature, this, silent: false);
    }

    // 升级：酒力 5 → 6，天意之力 4 → 5
    protected override void OnUpgrade()
    {
        DynamicVars["drunken_might"].UpgradeValueBy(1);
        DynamicVars["heavens_force"].UpgradeValueBy(1);
    }
}
