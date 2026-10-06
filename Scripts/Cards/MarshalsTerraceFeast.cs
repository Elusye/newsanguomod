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

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到蜀汉卡池（只在本池注册，曹魏拿不到）
[RegisterCard(typeof(ShuHanCardPool))]
public class MarshalsTerraceFeast : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 5 点酒力，抽 1 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>(5m),
        new CardsVar(1)
    ];

    // 鼠标悬停时显示酒力提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    public MarshalsTerraceFeast() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/marshals_terrace_feast");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得酒力
        int drunkenMightAmount = DynamicVars["DrunkenMightPower"].IntValue;
        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext,
            base.Owner.Creature,
            drunkenMightAmount,
            base.Owner.Creature,
            this,
            silent: false);

        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, base.Owner);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 酒力层数从 5 提高到 6 (5+1)
        DynamicVars["DrunkenMightPower"].UpgradeValueBy(1);

        // 抽牌数从 1 提高到 2 (1+1)
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
