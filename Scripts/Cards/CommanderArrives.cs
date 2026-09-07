using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Patches;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class CommanderArrives : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 9 点伤害；被变化/消耗时获得 2 点力量
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(9m, ValueProp.Move),
        new PowerVar<StrengthPower>("StrengthPower", 2)
    ];

    // 鼠标悬停时展示力量与消耗说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    public CommanderArrives() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        NewsanguoSfx.Play("event:/newsanguo/sfx/commander_arrives");

        // 播放角色攻击动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Attack", base.Owner.Character.CastAnimDelay);

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);
    }

    // 此牌被消耗时（仅战斗中）：获得力量
    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card == this && base.CombatState != null)
        {
            await GrantStrength(choiceContext);
        }
    }

    // 此牌被变化时（仅战斗中）：获得力量
    // AfterTransformedFrom 是同步 void 钩子，无法 await，因此异步放飞并捕获异常
    // 注意：变化流程会先把原卡移出牌堆，导致 base.CombatState 为 null（其依赖卡牌所在牌堆），
    // 因此改用 Owner.Creature.CombatState 判断是否处于战斗中
    public override void AfterTransformedFrom()
    {
        if (base.Owner.Creature.CombatState != null)
        {
            _ = GrantStrengthAsync(new ThrowingPlayerChoiceContext());
        }
    }

    private async Task GrantStrength(PlayerChoiceContext choiceContext)
    {
        // 同上：用 Owner.Creature.CombatState 判断是否处于战斗中
        if (base.Owner.Creature.CombatState is null)
        {
            return;
        }

        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["StrengthPower"].IntValue,
            base.Owner.Creature,
            this);
    }

    private async Task GrantStrengthAsync(PlayerChoiceContext choiceContext)
    {
        try
        {
            await GrantStrength(choiceContext);
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[commander_arrives] 被变化时获得力量失败: {e}");
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 9 提高到 12
        DynamicVars.Damage.UpgradeValueBy(3m);
        // 力量从 2 提高到 3（增加数值与原版一致：+1）
        DynamicVars["StrengthPower"].UpgradeValueBy(1);
    }
}
