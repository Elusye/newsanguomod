using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 军杖：衍生攻击牌，造成 2（3）点伤害 2 次，消耗
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class MilitaryCudgel : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每次打击伤害 2（升级 3）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(2m, ValueProp.Move)
    ];

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public MilitaryCudgel() : base(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/military_cudgel");

        // 播放角色攻击动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Attack", base.Owner.Character.CastAnimDelay);

        // 造成 2（3）点伤害 2 次
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(2)
            .Execute(choiceContext);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 每次打击伤害从 2 提高到 3
        DynamicVars.Damage.UpgradeValueBy(1m);
    }

    // 生成 1 张军杖并加入手牌（供其它卡牌调用，参考原版 Shiv.CreateInHand）
    public static async Task<CardModel?> CreateInHand(Player owner, ICombatState combatState)
    {
        // 战斗已结束或正在结束时不再生成，避免收尾阶段状态错乱
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return null;
        }

        CardModel cudgel = combatState.CreateCard<MilitaryCudgel>(owner);
        await CardPileCmd.AddGeneratedCardsToCombat([cudgel], PileType.Hand, owner);
        return cudgel;
    }
}
