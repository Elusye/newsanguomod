using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 注册卡牌到无色卡池（原版狂宴 Feed 的变体，额外添加“虚无”）
[RegisterCard(typeof(ColorlessCardPool))]
public class chow_down : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：衍生
    private const CardRarity rarity = CardRarity.Token;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：伤害与回复的生命值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new HealVar(2m)
    ];

    // 衍生牌不应出现在无色牌随机生成（无色药水、类星体、光谱偏移等）中
    public override bool CanBeGeneratedInCombat => false;

    public chow_down() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/chow_down");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 造成伤害并回复生命
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bite", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await CreatureCmd.Heal(owner.Creature, DynamicVars.Heal.BaseValue);
    }

    // 升级：伤害 7 → 8，回复 2 → 3
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Heal.UpgradeValueBy(1m);
    }
}
