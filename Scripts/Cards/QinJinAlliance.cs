using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using newsanguo.Scripts.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 卡池归属由注册特性决定；保留原类名与存档 ID。
[RegisterCard(typeof(NewsanguoCardPool))]
public class QinJinAlliance : NewsanguoCardTemplate
{

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("StrengthLoss", 8)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public QinJinAlliance() : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        NewsanguoSfx.Play("event:/newsanguo/sfx/qin_jin_alliance");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        int loss = DynamicVars["StrengthLoss"].IntValue;
        await PowerCmd.Apply<QinJinAllianceStrengthPower>(choiceContext, Owner.Creature, loss, Owner.Creature, this);
        await PowerCmd.Apply<QinJinAllianceStrengthPower>(choiceContext, cardPlay.Target, loss, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthLoss"].UpgradeValueBy(3);
}
