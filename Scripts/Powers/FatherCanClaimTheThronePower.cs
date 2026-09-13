using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “称帝”：在你的回合开始时，获得与层数相同的能量并额外抽等量牌。
/// 每次打出都会叠加 1 层；天意之力的增减由卡牌另行施加的“天意致胜”正/负层数承担。
/// </summary>
[RegisterPower]
public class FatherCanClaimTheThronePower : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 层数即每回合额外获得的能量与抽牌数（多次打出可叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合开始钩子（仅用于图标闪烁与音效反馈）需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.ForEnergy(this)];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if(player != Owner.Player)
        {
            return amount;
        }
        return amount + Amount;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if(player != Owner.Player)
        {
            return count;
        }
        return count + Amount;
    }

    // 数值效果由上面的 ModifyMaxEnergy / ModifyHandDraw 被动修正完成，没有显式触发点；
    // 因此音效放在回合开始的表现钩子里：图标闪一下并播放“称帝”音效，提示本回合加成已生效。
    // 对应 FMOD 事件 event:/newsanguo/sfx/father_can_claim_the_throne_power
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player is null || player.Creature != Owner || Amount <= 0)
        {
            return;
        }

        Flash();
        NewsanguoSfx.Play("event:/newsanguo/sfx/father_can_claim_the_throne_power");
    }
}
