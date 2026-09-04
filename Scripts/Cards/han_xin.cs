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

// 注册卡牌到无色卡池（衍生牌，可由特定效果生成）
[RegisterCard(typeof(ColorlessCardPool))]
public class han_xin : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：衍生
    private const CardRarity rarity = CardRarity.Token;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：抽 2 张牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(2)
    ];

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    // 衍生牌不应出现在无色牌随机生成（无色药水、类星体、光谱偏移等）中
    public override bool CanBeGeneratedInCombat => false;

    public han_xin() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        NewsanguoSfx.Play("event:/newsanguo/sfx/han_xin");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 抽 2（升级后 3）张牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, owner);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 抽牌数 2 → 3
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
