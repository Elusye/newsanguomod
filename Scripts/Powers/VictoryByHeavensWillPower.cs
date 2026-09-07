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
/// “天意致胜”：在你的回合开始时，使天意之力变动与层数相同的点数（正为获得、负为失去）。
/// </summary>
[RegisterPower]
public class VictoryByHeavensWillPower : ModPowerTemplate
{
    // 正面效果（负层数时表现为天意流失）
    public override PowerType Type => PowerType.Buff;
    // 层数即每回合天意之力的变动量，可为正也可为负（多次施加可叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 允许负数：卡牌可施加负层数（如“恭喜爹可以称帝了”施加 -5），表现为每回合失去天意之力
    public override bool AllowNegative => true;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 回合开始时：使天意之力变动与层数相同的点数（正为获得、负为失去）
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner || Amount == 0)
        {
            return;
        }

        if (Amount > 0)
        {
            // 触发音效：回合开始时获得天意之力
            NewsanguoSfx.Play("event:/newsanguo/sfx/victory_by_heavens_will_power");
        }
        else
        {
            // 负层数（天意流失）：改播流失音效，避免“获得”音效误导
            NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_force_decay");
        }

        await PowerCmd.Apply<HeavensForce>(choiceContext, Owner, Amount, Owner, null, silent: false);
    }
}
