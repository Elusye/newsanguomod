using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “亵渎天意”的延迟标记：下个回合结束时，获得15层天意侵蚀。
/// 打出亵渎天意时获得2层；每个玩家回合结束时层数-1，
/// 减到0时（即下个回合结束时）获得15层天意侵蚀并移除自身。
/// </summary>
[RegisterPower]
public class blasphemy_debt : ModPowerTemplate
{
    // 负面标记
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：计数器，Amount 表示剩余需要经历的回合结束次数
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 AfterSideTurnEnd 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 到期施加的天意侵蚀层数（默认 15，可由“亵渎天意”打出时传入调整）
    private int decayAmount = 15;

    // 设置到期施加的天意侵蚀层数（由“亵渎天意”卡牌传入，保持卡牌描述与数值同步）
    public void SetDecayAmount(int amount)
    {
        decayAmount = amount;
    }

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 玩家回合结束时：层数-1，减到0时获得15层天意侵蚀（层数归0后能力自动移除）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
        {
            return;
        }

        await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null, silent: true);

        if (Amount <= 0)
        {
            // 亵渎债务到期触发音效（对应 FMOD 事件 event:/newsanguo/sfx/blasphemy_debt）
            NewsanguoSfx.Play("event:/newsanguo/sfx/blasphemy_debt");
            await PowerCmd.Apply<heavens_decay_power>(choiceContext, Owner, decayAmount, Owner, null, silent: false);
        }
    }
}
