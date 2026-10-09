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

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 2026-10-05：按要求从「曹魏」（新三国）卡池移除，改为只注册在蜀汉卡池
// （同时已从 NewsanguoCardPool.CardTypes 中去掉，避免仍然命中曹魏池）
[RegisterCard(typeof(ShuHanCardPool))]
public class Invincible : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：对符合条件的敌人造成伤害增加 50%（升级后 75%）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("BonusPercent", 50)
    ];

    public Invincible() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 悬停提示：展示“飞行”、“振翅”与“翱翔”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<FlightPower>(),
        HoverTipFactory.FromPower<FlutterPower>(),
        HoverTipFactory.FromPower<SoarPower>()
    ];

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/invincible");

        // 本卡不属于天意体系：不失去天意之力（IsHeavensCard 保持默认 false，不计入「恨天剑法」的天意牌统计）

        // 附加“天下无敌”能力：对没有振翅和翱翔的敌人造成伤害增加 BonusPercent%（基础 25%，升级 50%）
        // 效果可叠加：每次打出都会叠加对应百分比的增伤
        int bonusPercent = DynamicVars["BonusPercent"].IntValue;
        await PowerCmd.Apply<InvinciblePower>(choiceContext, base.Owner.Creature, bonusPercent, base.Owner.Creature, this);
    }

    // 升级后的效果逻辑：增伤层数 50% → 75%
    protected override void OnUpgrade()
    {
        DynamicVars["BonusPercent"].UpgradeValueBy(25);
    }
}
