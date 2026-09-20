using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class FortySixtyTax : NewsanguoCardTemplate
{

    // 不可通过战斗内的变化/随机生成获得（防止利用金币效果刷金）
    public override bool CanBeGeneratedInCombat => false;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 卡牌基础数值：对目标造成 18 点伤害；获得目标当前血量 40% 的金币
    // TaxGold 只用于卡面预览（瞄准敌人时显示“将获得多少金币”），实际结算走 ComputeGold
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(18m, ValueProp.Move),
        new IntVar("TaxPercent", 40),
        new TaxGoldVar()
    ];

    // 金币收益的唯一定义处：卡面预览（TaxGoldVar）与打出结算都调用它，避免两处口径漂移。
    // 操作顺序与旧实现一致：先按百分比向下取整，再按游戏人数向下取整。
    internal static int ComputeGold(Creature target, int taxPercent, int playerCount)
    {
        if (playerCount <= 0)
        {
            playerCount = 1;
        }
        return target.CurrentHp * taxPercent / 100 / playerCount;
    }

    // 悬停提示：展示“消耗”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    public FortySixtyTax() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        ICombatState combatState = base.CombatState!;

        NewsanguoSfx.Play("event:/newsanguo/sfx/forty_sixty_tax");

        // 按伤害前血量计算：获得目标当前血量 TaxPercent% 的金币，除以游戏人数（向下取整）
        int playerCount = combatState.Players.Count;
        int gold = ComputeGold(target, DynamicVars["TaxPercent"].IntValue, playerCount);

        // 对目标造成 18 点伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_coin_explosion_regular", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        if (gold > 0)
        {
            await PlayerCmd.GainGold(gold, base.Owner);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 18 提高到 23
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
