using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class InvokeHeaven : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 5 点天意之力（升级后 7 点）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HeavensForce>("heavens_force", 5)
    ];

    // 悬停提示：展示“天意之力”和“天意侵蚀”两个说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForce>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    public InvokeHeaven() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（资源文件 invoke_heaven）
        NewsanguoSfx.Play("event:/newsanguo/sfx/invoke_heaven");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得天意之力
        await PowerCmd.Apply<HeavensForce>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["heavens_force"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑：天意之力 5 → 6
    protected override void OnUpgrade()
    {
        DynamicVars["heavens_force"].UpgradeValueBy(1);
    }
}
