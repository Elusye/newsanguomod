using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
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
// 写法照搬原版 ExpectAFight，唯一例外：**不施加 NoEnergyGainPower**
// （Old 系列要求：不给玩家新增"本回合不能再获得能量"的限制）
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class OldExpectAFight : NewsanguoCardTemplate
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<IroncladCardPool>();

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：照搬原版 ExpectAFight —— EnergyVar 固定为 0，
    // 实际获得的能量由 CalculatedEnergy（CalculationBase 0 + CalculationExtra 1 × 手牌攻击牌数）现算
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(0),
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

    // 悬停提示：能量说明（照搬原版 ExpectAFight 的 ExtraHoverTips）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        base.EnergyHoverTip
    ];

    // 打出时的效果逻辑（照搬原版 ExpectAFight）
    // 例外：**不施加 NoEnergyGainPower**（本 mod 的 Old 系列要求"不给玩家新增本回合不再获得能量的限制"）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 手牌中每有一张攻击牌就获得 1 点能量（数值来自 CalculatedEnergy，与原版同一算法）
        await PlayerCmd.GainEnergy(((CalculatedVar)DynamicVars["CalculatedEnergy"]).Calculate(cardPlay.Target), base.Owner);
    }

    // 升级后的效果逻辑：费用 2 → 1
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
