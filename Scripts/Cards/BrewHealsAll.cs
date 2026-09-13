using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
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

    // 酒力足以支付消耗（未升级 ≥6、升级后 ≥4）时金色高亮
    protected override bool ShouldGlowGoldInternal => IsUpgraded
        ? Owner.Creature.GetPowerAmount<DrunkenMightPower>() > 3
        : Owner.Creature.GetPowerAmount<DrunkenMightPower>() > 5;

    // 卡牌基础数值：消耗的酒力（升级后 4）、获得的再生层数
    // 消耗用的酒力用 IntVar（而非 PowerVar<DrunkenMightPower>）：它是“失去”数值，
    // 不应参与 PowerVar 的卡面预览钩子，否则会被“换大盏”错误地加高显示
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("DrunkenMight", 6),
        new PowerVar<RegenPower>(5m)
    ];

    // 悬停提示：展示“酒力”与“再生”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HoverTipFactory.FromPower<RegenPower>()
    ];

    public BrewHealsAll() :
        base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 酒力不足以支付卡面消耗（未升级 6、升级后 4）则本次打出无效果
        if (Owner.Creature.GetPowerAmount<DrunkenMightPower>() < DynamicVars["DrunkenMight"].IntValue) return;

        // 播放出牌语音
        NewsanguoSfx.Play("event:/newsanguo/sfx/brew_heals_all");

        // 消耗酒力并回复（获得再生）
        await PowerCmd.Apply<DrunkenMightPower>(choiceContext, Owner.Creature, -DynamicVars["DrunkenMight"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, DynamicVars["RegenPower"].BaseValue, Owner.Creature, this);
    }

    // 升级：消耗的酒力 6 → 4
    protected override void OnUpgrade()
    {
        DynamicVars["DrunkenMight"].UpgradeValueBy(-2m);
    }
}
