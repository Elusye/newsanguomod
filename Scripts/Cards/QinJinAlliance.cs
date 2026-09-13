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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class QinJinAlliance : NewsanguoCardTemplate
{

    // 鼠标悬停时显示格挡提示
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：你获得 10（13）点格挡；目标敌人获得固定的 5 点格挡和 1 层残影
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(10, ValueProp.Move),
        new IntVar("EnemyBlock", 5),
        new PowerVar<BlurPower>(1m)
    ];

    // 鼠标悬停时展示格挡与残影说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<BlurPower>()
    ];

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

        // 你获得 10（13）点格挡（受敏捷/脆弱正常影响）
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, fast: true);

        // 目标敌人获得固定的 5 点格挡（Move|Unpowered 使敏捷与脆弱不参与修正）
        await CreatureCmd.GainBlock(cardPlay.Target, DynamicVars["EnemyBlock"].IntValue, ValueProp.Move | ValueProp.Unpowered, cardPlay, fast: true);

        // 目标敌人获得 1 层残影
        await PowerCmd.Apply<BlurPower>(choiceContext, cardPlay.Target, DynamicVars["BlurPower"].IntValue, base.Owner.Creature, this);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 格挡从 10 提高到 13
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
