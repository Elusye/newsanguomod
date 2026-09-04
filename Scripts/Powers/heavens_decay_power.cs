using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天意侵蚀”：你的下X个回合会被天意爷接管。
/// 回合开始时，层数-1，然后从右到左自动打出你的手牌（上限13张）。
/// 类名带 _power 后缀以区别于同名的卡牌 heavens_decay（诅咒“天意侵蚀”）。
/// </summary>
[RegisterPower]
public class heavens_decay_power : ModPowerTemplate
{
    // 负面效果：回合被接管
    public override PowerType Type => PowerType.Debuff;
    // 叠加方式：计数器，Amount 表示剩余被接管的回合数
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 AfterAutoPrePlayPhaseEntered 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 生物能力列表是引擎回合钩子的触发顺序（玩家能力栏从左到右 = 该列表从前到后）。
    // 要让“天意侵蚀”在每个回合开始比其他能力更早触发，需把它始终放在能力列表最左侧（索引 0）。
    private static readonly FieldInfo PowersField =
        typeof(Creature).GetField("_powers", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(Creature).FullName, "_powers");

    // 能力图标资源（图标文件保持 heavens_decay 命名，不随类名变更）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/heavens_decay.png",
        BigIconPath: "res://newsanguo/images/powers/heavens_decay_big.png"
    );

    // 施加/层数变化时，把自己移到能力列表最左侧，确保回合开始时最优先触发
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        EnsureLeftmostPower();
    }

    // 把自己移到生物能力列表的索引 0（能力栏最左侧）。
    // 引擎只在末尾追加新能力，因此只要在本能力被施加/叠层时调整一次，之后始终保持在最左，
    // 直到本能力被移除为止。引擎在客户端回放相同命令，各端表现一致。
    private void EnsureLeftmostPower()
    {
        if (Owner is not { } owner)
        {
            return;
        }
        if (PowersField.GetValue(owner) is not List<PowerModel> powers)
        {
            return;
        }
        int index = powers.IndexOf(this);
        if (index <= 0)
        {
            return;
        }
        powers.RemoveAt(index);
        powers.Insert(0, this);
        // 触发一次“能力变化”事件，让能力栏 UI 按新顺序（本能力在最左）重新布局
        owner.InvokePowerModified(this, 1, silent: true);
    }

    // 回合开始时：层数-1，然后天意爷接管，从右到左自动打出所有能打出的手牌
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner)
        {
            return;
        }

        // 兜底：确保本回合触发钩子前仍处于能力列表最左侧（正常情况下由 AfterApplied 维持）
        EnsureLeftmostPower();

        // 天意侵蚀触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_decay）
        NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_decay");

        // 天意侵蚀层数-1（减到0后能力自动移除）
        await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null, silent: true);

        // 天意爷接管：从右到左自动打出手牌（上限13张）
        await AutoPlayRightToLeft.PlayHandRightToLeftAsync(choiceContext, player);
    }
}
