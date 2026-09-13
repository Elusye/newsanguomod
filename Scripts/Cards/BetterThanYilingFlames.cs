using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class BetterThanYilingFlames : NewsanguoCardTemplate
{
    // 本场战斗中累计由“打出本卡”获得的额外伤害（用于降级后恢复数值）
    private decimal _extraDamageFromPlays;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：基础伤害 9，每打出一次所有复制品的伤害增加 3（升级后 4）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new DynamicVar("Increase", 3m)
    ];

    public BetterThanYilingFlames() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑（参考原版 Claw：打出后本场战斗所有复制品伤害 +Increase）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/better_than_yiling_flames");

        // 在目标脚下燃起火焰特效
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(cardPlay.Target));

        // 造成当前伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 将一张此牌的复制品加入你的弃牌堆（用 clone 而非 dupe：dupe 打出后会直接移出战斗，无法循环回弃牌堆）
        // 与原版 Anger 一致：AddGeneratedCardToCombat 本身不会更新弃牌堆 UI 计数，
        // 需配合 PreviewCardPileAdd 生成飞行预览，动画结束时触发 InvokeCardAddFinished 使弃牌堆计数 +1
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, base.Owner, CardPilePosition.Top), 2.2f);

        // 本场战斗中所有此牌卡牌的伤害增加（含刚加入弃牌堆的复制品）
        decimal increase = base.DynamicVars["Increase"].BaseValue;
        foreach (BetterThanYilingFlames item in base.Owner.PlayerCombatState.AllCards.OfType<BetterThanYilingFlames>())
        {
            item.BuffFromPlay(increase);
        }
    }

    // 升级：基础伤害 9 → 11，每次额外增加 3 → 4
    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(2m);
        base.DynamicVars["Increase"].UpgradeValueBy(1m);
    }

    // 降级后 DynamicVars 会从卡池初始模型重建，需把本场战斗累计的额外伤害补回去
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        base.DynamicVars.Damage.BaseValue += _extraDamageFromPlays;
    }

    // 从一次打出中为本卡增加额外伤害
    private void BuffFromPlay(decimal extraDamage)
    {
        base.DynamicVars.Damage.BaseValue += extraDamage;
        _extraDamageFromPlays += extraDamage;
    }
}
