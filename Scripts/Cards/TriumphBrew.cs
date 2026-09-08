using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 多人牌：注册到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class TriumphBrew : NewsanguoCardTemplate
{

    // 仅多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    public TriumphBrew() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/triumph_brew");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得“痛饮庆功酒”能力：每当你获得酒力时，其他盟友获得等量酒力
        await PowerCmd.Apply<TriumphBrewPower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 升级：获得“固有”（战斗开始时该牌必定在手牌中）
        AddKeyword(CardKeyword.Innate);
    }

    // 降级后的效果逻辑（升级被移除或回退时调用）
    protected override void AfterDowngraded()
    {
        RemoveKeyword(CardKeyword.Innate);
    }
}
