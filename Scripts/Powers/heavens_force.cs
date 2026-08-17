using System.Collections.Generic;
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

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天意之力”：计数效果。
/// 玩家回合结束时：若层数不小于10，执行一次原“天意之助”效果（1层“双倍伤害”+1个额外回合），然后层数-10；
/// 若层数不大于-10，获得1层“天意侵蚀”，然后层数+10。
/// 每次最多转化10层（例如33 → 23），余数保留继续累计。
/// </summary>
[RegisterPower]
public class heavens_force : ModPowerTemplate
{
    // 标记本回合结束是否触发了正向转化（授予额外回合），引擎随后询问 ShouldTakeExtraTurn 时读取并清除
    private bool _grantExtraTurn;

    // 计数能力：Buff 分类，层数可正可负
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 允许负数，用于表示天意侵蚀方向
    public override bool AllowNegative => true;
    // 允许接收战斗钩子，否则 AfterSideTurnEnd / ShouldTakeExtraTurn 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 悬停天意之力时，同时展示天意侵蚀的效果说明，便于玩家了解负向转化
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<heavens_decay>()];

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
            SfxCmd.Play("event:/newsanguo/sfx/heavens_force");

            // 正向转化：执行一次原“天意之助”效果（1层“双倍伤害”+1个额外回合）。
            // 注意：不能在这里先把层数-10——若恰好10层，扣到0后能力会被立即移除，
            // 引擎随后询问 ShouldTakeExtraTurn 时将收不到本能力（这正是之前“天意之助”
            // 仅1层时无法获得额外回合的根因）。因此扣层数推迟到 AfterTakingExtraTurn
            // （额外回合被确认授予后）再进行。
            _grantExtraTurn = true;
            await PowerCmd.Apply<DoubleDamagePower>(choiceContext, Owner, 1, Owner, null, silent: false);
        }
        else if (Amount <= -10)
        {
            // 天意之力负向转化触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_force_decay）
            SfxCmd.Play("event:/newsanguo/sfx/heavens_force_decay");

            // 负向转化：获得1层天意侵蚀，天意之力+10（例如-33 → -23；-10 → 0 时能力自然移除，不影响额外回合判定）
            await PowerCmd.Apply<heavens_decay>(choiceContext, Owner, 1, Owner, null, silent: false);
            await PowerCmd.ModifyAmount(choiceContext, this, 10, Owner, null, silent: true);
        }
    }

    // 引擎在玩家回合结束后询问是否获得额外回合（正向转化时返回true，参考原版遗物“佩尔之眼”）
    public override bool ShouldTakeExtraTurn(Player player)
    {
        if (player?.Creature != Owner)
        {
            return false;
        }
        bool grant = _grantExtraTurn;
        _grantExtraTurn = false;
        return grant;
    }

    // 引擎确认授予额外回合后，才扣除本次正向转化消耗的10层（扣到0能力自动移除，不影响本回合判定）
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player?.Creature != Owner)
        {
            return;
        }
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -10, Owner, null, silent: true);
    }
}
