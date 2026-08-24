using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

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

    // 能力图标资源（图标文件保持 heavens_decay 命名，不随类名变更）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/heavens_decay.png",
        BigIconPath: "res://newsanguo/images/powers/heavens_decay_big.png"
    );

    // 回合开始时：层数-1，然后天意爷接管，从右到左自动打出所有能打出的手牌
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner)
        {
            return;
        }

        // 天意侵蚀触发音效（对应 FMOD 事件 event:/newsanguo/sfx/heavens_decay）
        SfxCmd.Play("event:/newsanguo/sfx/heavens_decay");

        // 天意侵蚀层数-1（减到0后能力自动移除）
        await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null, silent: true);

        // 天意爷接管：从右到左自动打出手牌（上限13张）
        await AutoPlayRightToLeft.PlayHandRightToLeftAsync(choiceContext, player);
    }
}
