using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts;

[RegisterCard(typeof(NewsanguoCardPool))]
[RegisterCharacterStarterCard(typeof(NewsanguoCharacter), 1)]
public class blade_of_virtue : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：初始牌
    private const CardRarity rarity = CardRarity.Basic;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 3 点伤害，攻击 2 次；给予目标 1 层虚弱、1 层易伤
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3, ValueProp.Move),
        new RepeatVar(2),
        new PowerVar<WeakPower>("WeakPower", 1),
        new PowerVar<VulnerablePower>("VulnerablePower", 1)
    ];

    public blade_of_virtue() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 悬停提示：展示“虚弱”和“易伤”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/blade_of_virtue");

        // 对目标造成 4 点伤害 2 次
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target!)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .Execute(choiceContext);

        // 给予目标虚弱
        await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, DynamicVars["WeakPower"].IntValue, base.Owner.Creature, this, silent: false);

        // 给予目标易伤
        await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, DynamicVars["VulnerablePower"].IntValue, base.Owner.Creature, this, silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 每次伤害从 3 提高到 5
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}
