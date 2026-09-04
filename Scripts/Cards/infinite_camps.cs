using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class infinite_camps : NewsanguoCardTemplate
{
    // 基础耗能：1（升级后 0）
    private const int energyCost = 1;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
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

    public infinite_camps() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/infinite_camps");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 附加“无限连营”能力：层数 +1（可叠加）。
        // 每打出一张牌按“此前已打出的无限连营张数”计入下回合抽牌：
        // 第 1 张（施放者）此前为 0，不计入；第 2 张起在打出时按打出前的张数补计入。
        infinite_camps_power? power = await PowerCmd.Apply<infinite_camps_power>(choiceContext, owner.Creature, 1, owner.Creature, this);
        if (power is null)
        {
            return;
        }

        // 打出前已有的“无限连营”张数（= 叠加后的层数 - 本张）
        int priorCopies = power.Amount - 1;
        power.MarkAppliedBy(this);
        if (priorCopies > 0)
        {
            await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, owner.Creature, priorCopies, owner.Creature, this);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 费用从 1 降低到 0
        EnergyCost.UpgradeBy(-1);
    }
}
