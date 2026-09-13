using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 跃跃欲试（旧）：旧版本未被削弱的跃跃欲试，手牌中每有一张攻击牌就获得能量
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class OldExpectAFight : NewsanguoCardTemplate
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<IroncladCardPool>();

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每张攻击牌获得的能量；战斗中动态计算本次可获得的总能量
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedEnergy").WithMultiplier(static (card, _) =>
        {
            Player? owner = card.Owner;
            return owner is null ? 0m : CardPile.Get(PileType.Hand, owner)!.Cards.Count(c => c.Type == CardType.Attack);
        })
    ];

    public OldExpectAFight() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 手牌中每有一张攻击牌，就获得 1 点能量
        int attackCount = CardPile.Get(PileType.Hand, base.Owner)!.Cards.Count(c => c.Type == CardType.Attack);
        if (attackCount > 0)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue * attackCount, base.Owner);
        }
    }

    // 升级后的效果逻辑：费用 2 → 1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
