using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天意致胜”：在你的回合开始时，获得与层数相同的天意之力。
/// </summary>
[RegisterPower]
public class victory_by_heavens_will_power : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 层数即每回合获得的天意之力（多次打出可叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 回合开始时：获得与层数相同的天意之力
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner || Amount <= 0)
        {
            return;
        }

        // 触发音效：回合开始时获得天意之力
        NewsanguoSfx.Play("event:/newsanguo/sfx/victory_by_heavens_will_power");

        await PowerCmd.Apply<heavens_force>(choiceContext, Owner, Amount, Owner, null, silent: false);
    }
}
