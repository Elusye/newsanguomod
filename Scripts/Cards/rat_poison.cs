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
public class rat_poison : NewsanguoCardTemplate
{
    // 基础耗能：1（常驻一费，升级不改变耗能）
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

    // 悬停提示：展示“毒鼠”说明（升级后展示升级版毒鼠）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<poison_rat>(IsUpgraded)
    ];

    public rat_poison() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/rat_poison");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 附加能力：每回合开始时将一张毒鼠加入手牌。
        // 升级版附加“毒鼠计+”能力，生成升级版（毒鼠+）。
        if (IsUpgraded)
        {
            await PowerCmd.Apply<rat_poison_plus_power>(
                choiceContext,
                base.Owner.Creature,
                1,
                base.Owner.Creature,
                this,
                silent: false);
        }
        else
        {
            await PowerCmd.Apply<rat_poison_power>(
                choiceContext,
                base.Owner.Creature,
                1,
                base.Owner.Creature,
                this,
                silent: false);
        }
    }
}
