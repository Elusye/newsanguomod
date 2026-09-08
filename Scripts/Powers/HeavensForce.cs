using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天意之力”：计数效果。
/// 玩家回合结束时：若层数不小于10，消耗10层并触发一次正向转化（击晕所有敌人，
/// 下个回合造成双倍伤害——由原版 ShadowStepPower 实现，避免再走“额外回合”流程）；
/// 若层数不大于-10，获得1层“天意侵蚀”，然后层数+10。
/// 每次最多转化10层（例如33 → 23），余数保留继续累计。
/// </summary>
[RegisterPower]
public class HeavensForce : ModPowerTemplate
{
    // 计数能力：Buff 分类，层数可正可负
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 允许负数，用于表示天意侵蚀方向
    public override bool AllowNegative => true;
    // 允许接收战斗钩子，否则 AfterSideTurnEnd 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 悬停天意之力时，同时展示天意侵蚀的效果说明，便于玩家了解负向转化
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<HeavensDecayPower>()];

    // 玩家回合结束时：每回合最多转化一次（10层换1次效果），余数保留
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }
        if (Amount == 0)
        {
            return;
        }

        if (Amount >= 10)
        {
            // 天意之力正向转化触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_force）
            NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_force");

            // 直接扣除本次正向转化消耗的10层（扣到0能力自动移除，不影响本次效果）
            await PowerCmd.ModifyAmount(choiceContext, this, -10, Owner, null, silent: true);

            // 击晕所有敌人
            if (Owner.CombatState is { } combatState)
            {
                foreach (Creature enemy in combatState.GetOpponentsOf(Owner).Where(c => c.IsAlive))
                {
                    await CreatureCmd.Stun(enemy);
                }
            }

            // 下个回合造成双倍伤害：施加原版 ShadowStepPower，它在下次己方回合开始时
            // 施加等量 DoubleDamagePower 后自行移除，不再涉及额外回合流程
            await PowerCmd.Apply<ShadowStepPower>(choiceContext, Owner, 1, Owner, null, silent: false);
        }
        else if (Amount <= -10)
        {
            // 天意之力负向转化触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_force_decay）
            NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_force_decay");

            // 负向转化：获得1层天意侵蚀，天意之力+10（例如-33 → -23；-10 → 0 时能力自然移除）
            await PowerCmd.Apply<HeavensDecayPower>(choiceContext, Owner, 1, Owner, null, silent: false);
            await PowerCmd.ModifyAmount(choiceContext, this, 10, Owner, null, silent: true);
        }
    }
}
