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
// 与 Powers 命名空间内的能力类 dragon_omen 区分，别名指向卡牌类
using dragon_omen_card = newsanguo.Scripts.dragon_omen;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “风从虎，云从龙+”：由升级版“风从虎，云从龙”获得。
/// 每回合开始时，将 Amount 张“笑面虎+”和 Amount 张“龙可是帝王之征啊+”加入手牌。
/// </summary>
[RegisterPower]
public class wind_of_tiger_plus_power : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示每回合生成的组数（每打出一次 +1）
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

    // 回合开始时（抽牌后），将笑面虎+和龙可是帝王之征啊+加入手牌
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

        // 触发音效：笑面虎+与龙可是帝王之征啊+加入手牌
        NewsanguoSfx.Play("event:/newsanguo/sfx/wind_of_tiger_power");

        for (int i = 0; i < Amount; i++)
        {
            CardModel tiger = combatState.CreateCard<smiling_tiger>(player);
            CardModel dragon = combatState.CreateCard<dragon_omen_card>(player);
            CardCmd.Upgrade(tiger);
            CardCmd.Upgrade(dragon);
            await CardPileCmd.AddGeneratedCardToCombat(tiger, PileType.Hand, player, CardPilePosition.Random);
            await CardPileCmd.AddGeneratedCardToCombat(dragon, PileType.Hand, player, CardPilePosition.Random);
        }
    }
}
