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
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class Invincible : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：对符合条件的敌人造成伤害增加 25%（升级后 50%）；打出时失去 3 点天意之力（升级后 2 点）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(3m),
        new IntVar("BonusPercent", 25)
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public Invincible() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 悬停提示：展示“飞行”、“振翅”、“翱翔”、“天意之力”与“天意侵蚀”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<FlightPower>(),
        HoverTipFactory.FromPower<FlutterPower>(),
        HoverTipFactory.FromPower<SoarPower>(),
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/invincible");

        // 打出时失去 3 点天意之力（升级后 2 点）
        await HeavensForce.Add(choiceContext, base.Owner, -DynamicVars["HeavensForcePower"].IntValue, this);

        // 附加“天下无敌”能力：对没有振翅和翱翔的敌人造成伤害增加 BonusPercent%（基础 25%，升级 50%）
        // 效果可叠加：每次打出都会叠加对应百分比的增伤
        int bonusPercent = DynamicVars["BonusPercent"].IntValue;
        await PowerCmd.Apply<InvinciblePower>(choiceContext, base.Owner.Creature, bonusPercent, base.Owner.Creature, this);
    }

    // 升级后的效果逻辑：失去的天意之力从 3 减少到 2，增伤层数 25% → 50%
    protected override void OnUpgrade()
    {
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
        DynamicVars["BonusPercent"].UpgradeValueBy(25);
    }
}
