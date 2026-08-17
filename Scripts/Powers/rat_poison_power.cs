using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “毒鼠计”：每回合开始时，将 Amount 张“毒鼠”加入手牌。
/// 升级版由“毒鼠计+”能力（rat_poison_plus_power）处理，生成升级版（毒鼠+）。
/// </summary>
[RegisterPower]
public class rat_poison_power : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示每回合生成的毒鼠数量（每打出一次 +1）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要回合开始钩子
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 回合开始时（抽牌后），将毒鼠加入手牌
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner is null || player != Owner.Player)
        {
            return;
        }

        ICombatState? combatState = CombatState;
        if (combatState is null || Amount <= 0)
        {
            return;
        }

        // 触发音效：毒鼠加入手牌
        SfxCmd.Play("event:/newsanguo/sfx/rat_poison_power");

        for (int i = 0; i < Amount; i++)
        {
            CardModel rat = combatState.CreateCard<poison_rat>(player);
            await CardPileCmd.AddGeneratedCardToCombat(rat, PileType.Hand, player, CardPilePosition.Random);
        }
    }
}
