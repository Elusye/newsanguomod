using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
// 注册卡牌到无色卡池（衍生牌，通过压缩（旧）变化生成）
[RegisterCard(typeof(ColorlessCardPool))]
public class old_fuel : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：衍生
    private const CardRarity rarity = CardRarity.Token;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的能量、抽牌数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new CardsVar(1)
    ];

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 衍生牌不应出现在无色牌随机生成（无色药水、类星体、光谱偏移等）中
    public override bool CanBeGeneratedInCombat => false;

    public old_fuel() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑（参考原版燃料 Fuel）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        // 获得能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, owner);

        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, owner);
    }

    // 升级：抽牌数 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
