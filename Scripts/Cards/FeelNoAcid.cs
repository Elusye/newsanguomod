using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
public class FeelNoAcid : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每当你失去酒力时，获得 2 点酒力（调整此处的 2 即可联动卡面描述）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<FeelNoAcidPower>("feel_no_acid_power", 2)
    ];

    public FeelNoAcid() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/feel_no_acid");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 附加咱家不怕酸能力，层数＝每次失去酒力时补偿的酒力数
        int compensate = DynamicVars["feel_no_acid_power"].IntValue;
        await PowerCmd.Apply<FeelNoAcidPower>(
            choiceContext,
            base.Owner.Creature,
            compensate,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 补偿酒力 2 → 3
        DynamicVars["feel_no_acid_power"].UpgradeValueBy(1);
    }
}
