using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到无色卡池（与“士兵”同池；衍生牌，仅通过毒鼠计等效果生成）
[RegisterCard(typeof(ColorlessCardPool))]
public class poison_rat : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：衍生
    private const CardRarity rarity = CardRarity.Token;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 不在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = false;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：给予 7 层中毒
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<PoisonPower>("PoisonPower", 7)
    ];

    // 悬停提示：展示“中毒”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<PoisonPower>()
    ];

    // 卡牌自带“虚无”和“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    // 衍生牌不应出现在战斗随机生成中
    public override bool CanBeGeneratedInCombat => false;

    public poison_rat() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is null)
        {
            return;
        }

        // 播放出牌音效
        SfxCmd.Play("event:/newsanguo/sfx/poison_rat");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 给予目标 7（10）层中毒
        await PowerCmd.Apply<PoisonPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["PoisonPower"].IntValue,
            base.Owner.Creature,
            this);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 中毒从 7 提高到 10
        DynamicVars["PoisonPower"].UpgradeValueBy(3);
    }
}
