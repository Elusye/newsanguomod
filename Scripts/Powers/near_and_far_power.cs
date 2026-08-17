using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “忽近忽远”：在接下来的回合内，交替获得与层数相同的临时敏捷和临时力量。
/// </summary>
[RegisterPower]
public class near_and_far_power : ModPowerTemplate
{
    private class Data
    {
        // 下一个回合先获得敏捷，之后交替
        public bool nextIsDexterity = true;
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 层数即每次交替获得的量（多次打出可叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 回合开始时：交替获得临时敏捷/力量
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player is null || player.Creature != Owner || Amount <= 0)
        {
            return;
        }

        Flash();

        // 触发音效：交替获得临时敏捷/力量
        SfxCmd.Play("event:/newsanguo/sfx/near_and_far_power");

        if (GetInternalData<Data>().nextIsDexterity)
        {
            await PowerCmd.Apply<near_and_far_dexterity_power>(choiceContext, Owner, Amount, Owner, null, silent: false);
        }
        else
        {
            await PowerCmd.Apply<near_and_far_strength_power>(choiceContext, Owner, Amount, Owner, null, silent: false);
        }

        // 翻转：下次获得另一种属性
        GetInternalData<Data>().nextIsDexterity = !GetInternalData<Data>().nextIsDexterity;
    }
}
