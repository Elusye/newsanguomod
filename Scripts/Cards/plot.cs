using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class plot : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：普通
    private const CardRarity rarity = CardRarity.Common;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 6 点伤害；满足条件时额外抽 1 张牌（升级不再增加抽牌数）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move),
        new CardsVar(1)
    ];

    // 当前手牌数恰好为 3 时金色高亮（打出后剩 2，可触发额外抽牌）
    protected override bool ShouldGlowGoldInternal =>
        PileType.Hand.GetPile(base.Owner).Cards.Count == 3;

    public plot() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        Creature? target = cardPlay.Target;
        if (owner is null || target is null)
        {
            return;
        }

        // 播放出牌音效
        SfxCmd.Play("event:/newsanguo/sfx/plot");

        // 播放角色攻击动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Attack", owner.Character.CastAnimDelay);

        // 造成 6 点伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        // 若打出这张牌后手牌数为 2，则额外抽 1 张牌
        // （打出时这张牌已离开手牌，此时的手牌数即为“打出后”的手牌数）
        if (PileType.Hand.GetPile(owner).Cards.Count == 2)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, owner);
        }
    }

    // 升级：伤害 6 → 9（抽牌效果不再升级）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
