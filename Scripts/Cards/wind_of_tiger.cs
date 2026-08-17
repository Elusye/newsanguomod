using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class wind_of_tiger : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：能力
    private const CardType type = CardType.Power;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 悬停提示：展示“笑面虎”和“龙可是帝王之征啊”（升级后展示升级版）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<smiling_tiger>(IsUpgraded),
        HoverTipFactory.FromCard<dragon_omen>(IsUpgraded)
    ];

    public wind_of_tiger() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        SfxCmd.Play("event:/newsanguo/sfx/wind_of_tiger");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 附加能力：每回合开始时将笑面虎和龙可是帝王之征啊加入手牌。
        // 升级版附加“风从虎，云从龙+”能力，生成升级版（笑面虎+/龙可是帝王之征啊+）。
        if (IsUpgraded)
        {
            await PowerCmd.Apply<wind_of_tiger_plus_power>(
                choiceContext,
                base.Owner.Creature,
                1,
                base.Owner.Creature,
                this,
                silent: false);
        }
        else
        {
            await PowerCmd.Apply<wind_of_tiger_power>(
                choiceContext,
                base.Owner.Creature,
                1,
                base.Owner.Creature,
                this,
                silent: false);
        }
    }
}
