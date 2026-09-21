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
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 燃料（旧）：旧版本的燃料，获得能量并抽牌，由压缩（旧）生成
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class OldFuel : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的能量、抽牌数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new CardsVar(1)
    ];

    // 悬停提示：能量说明（照搬原版 Fuel 的 ExtraHoverTips）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.ForEnergy(this)
    ];

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public OldFuel() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    // 打出时的效果逻辑（参考原版燃料 Fuel）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 获得能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, base.Owner);

        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, base.Owner);
    }

    // 升级：抽牌数 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
