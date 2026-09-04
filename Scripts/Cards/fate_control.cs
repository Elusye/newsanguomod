using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class fate_control : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：能力
    private const CardType type = CardType.Power;
    // 卡牌稀有度：稀有
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：打出时失去 5 点天意之力（升级后 4 点）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<heavens_force>("heavens_force", 5)
    ];

    public fate_control() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/fate_control");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 打出时失去 5 点天意之力（升级后 4 点）
        await PowerCmd.Apply<heavens_force>(choiceContext, base.Owner.Creature, -DynamicVars["heavens_force"].IntValue, base.Owner.Creature, this);

        // 附加“天意操控”能力：所有在战斗中临时增加的牌将被升级
        await PowerCmd.Apply<fate_control_power>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑：失去的天意之力从 5 减少到 4（费用保持 0），并获得“固有”
    protected override void OnUpgrade()
    {
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
        AddKeyword(CardKeyword.Innate);
    }

    // 降级：移除“固有”
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Innate);
    }
}
