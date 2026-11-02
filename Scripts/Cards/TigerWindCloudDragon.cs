using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class TigerWindCloudDragon : NewsanguoCardTemplate
{
    public override bool IsTigerDragonCard => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 每层能力使每次打出龙虎牌抽 1 张牌。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<TigerWindCloudDragonPower>(1m)
    ];

    // 展示帝王之征与固定生成的两张龙虎牌。
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DragonOmenPower>(),
        HoverTipFactory.FromCard<SmilingTiger>(),
        this is TigerWindCloudDragonShuHan ? HoverTipFactory.FromCard<DragonOmenShuHan>() : HoverTipFactory.FromCard<DragonOmen>()
    ];
    // 构造函数
    public TigerWindCloudDragon() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        NewsanguoSfx.Play("event:/newsanguo/sfx/tiger_wind_cloud_dragon");

        // 获得抽牌能力。
        int amount = DynamicVars["TigerWindCloudDragonPower"].IntValue;
        await PowerCmd.Apply<TigerWindCloudDragonPower>(
            choiceContext,
            base.Owner.Creature,
            amount,
            base.Owner.Creature,
            this,
            silent: false);

        // 固定各生成一张，保留来源卡池的独立实体。
        bool useShuHanVariants = this is TigerWindCloudDragonShuHan;
        CardModel tiger = combatState.CreateCard<SmilingTiger>(Owner);
        CardModel dragon = useShuHanVariants
            ? combatState.CreateCard<DragonOmenShuHan>(Owner)
            : combatState.CreateCard<DragonOmen>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(tiger, PileType.Hand, Owner);
        await CardPileCmd.AddGeneratedCardToCombat(dragon, PileType.Hand, Owner);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 耗能从 1 降低到 0
        EnergyCost.UpgradeBy(-1);
    }
}
