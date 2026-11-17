using newsanguo.Scripts.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 保留旧曹魏卡牌 ID，供已有存档读取。
[RegisterCard(typeof(RetiredCardPool))]
public class TigerWindCloudDragon : NewsanguoCardTemplate
{
    public override bool CanBeGeneratedByModifiers => Pool is not RetiredCardPool;
    public override bool CanBeGeneratedInCombat => Pool is not RetiredCardPool;

    public override bool IsTigerDragonCard => !BalanceAdjustments.Enabled;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 每层能力提供 1 点重放。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<TigerWindCloudDragonPower>(1m)
    ];

    // 展示受影响的笑面虎与重放说明。
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<SmilingTiger>();
            if (BalanceAdjustments.Enabled)
                yield return HoverTipFactory.Static(StaticHoverTip.ReplayDynamic,
                    new DynamicVar("Times", DynamicVars["TigerWindCloudDragonPower"].IntValue));
            else
                yield return this is TigerWindCloudDragonShuHan
                    ? HoverTipFactory.FromCard<DragonOmenShuHan>()
                    : HoverTipFactory.FromCard<DragonOmen>();
        }
    }
    // 构造函数
    public TigerWindCloudDragon() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/tiger_wind_cloud_dragon");

        int amount = DynamicVars["TigerWindCloudDragonPower"].IntValue;
        await PowerCmd.Apply<TigerWindCloudDragonPower>(
            choiceContext,
            base.Owner.Creature,
            amount,
            base.Owner.Creature,
            this,
            silent: false);

        if (!BalanceAdjustments.Enabled)
        {
            var combat = CombatState!;
            var tiger = combat.CreateCard<SmilingTiger>(Owner);
            MegaCrit.Sts2.Core.Models.CardModel dragon = this is TigerWindCloudDragonShuHan
                ? combat.CreateCard<DragonOmenShuHan>(Owner)
                : combat.CreateCard<DragonOmen>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(tiger, PileType.Hand, Owner);
            await CardPileCmd.AddGeneratedCardToCombat(dragon, PileType.Hand, Owner);
        }

    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 耗能从 1 降低到 0
        EnergyCost.UpgradeBy(-1);
    }
}
