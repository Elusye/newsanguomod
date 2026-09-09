using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “真的要醉了”：由“真的是要醉了！”在打出时施加的一次性标记能力。
/// 在你的下个回合开始时：按层数（Amount）获得酒力，并施加“止戈”（该回合不能打出攻击牌），随后移除自身。
/// </summary>
[RegisterPower]
public class ImGettingDrunkPower : ModPowerTemplate
{
    // 正面效果；层数 = 下回合开始时获得的酒力
    public override PowerType Type => PowerType.Buff;
    // 多次打出可叠加（多次下回合酒力叠加发放）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合边界钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源：暂复用“酒力”图标（待生效的是酒力与禁攻），如需专属图标替换这两处路径即可
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/ImGettingDrunkPower.png",
        BigIconPath: "res://newsanguo/images/powers/ImGettingDrunkPower_big.png"
    );

    // 玩家回合开始：发放酒力、施加“止戈”，随后移除自身
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || Amount <= 0)
        {
            return;
        }

        // 触发音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/im_getting_drunk_power");

        // 发放酒力
        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext: null!,
            target: Owner,
            amount: Amount,
            applier: Owner,
            cardSource: null,
            silent: false);

        // 施加“止戈”：本回合（即你的下个回合）不能打出攻击牌
        await PowerCmd.Apply<NoAttacksThisTurnPower>(
            choiceContext: null!,
            target: Owner,
            amount: 1,
            applier: Owner,
            cardSource: null);

        // 一次性标记，触发后移除
        await PowerCmd.Remove(this);
    }
}
