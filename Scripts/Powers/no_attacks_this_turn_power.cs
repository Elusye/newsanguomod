using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “止戈”：本回合内不能打出攻击牌。
/// 由“接着奏乐接着舞”施加，玩家回合结束时自动移除。
/// 通过重写 ShouldPlay 拦截本回合所有攻击牌的打出（对应原版 Normality 的 BlockedByHook 机制）。
/// </summary>
[RegisterPower]
public class no_attacks_this_turn_power : ModPowerTemplate
{
    // 负面效果：Debuff
    public override PowerType Type => PowerType.Debuff;
    // 不叠加：存在即生效
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要战斗钩子
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 拦截打出：本回合内该玩家的攻击牌一律不可打出
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        return !(card.Type == CardType.Attack && card.Owner == Owner?.Player);
    }

    // 玩家回合结束时移除自身
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return;
        }

        await PowerCmd.Remove(this);
    }
}
