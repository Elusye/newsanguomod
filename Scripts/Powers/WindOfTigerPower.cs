using System.Threading.Tasks;
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
/// “风从虎，云从龙”：层数即每当你打出一张“笑面虎”或“龙可是帝王之征啊”时抽取的牌数。
/// </summary>
[RegisterPower]
public class WindOfTigerPower : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示每次触发抽的牌数（每打出一次“风从虎，云从龙”叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要在牌被打出后被咨询
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 每当你打出一张“笑面虎”或“龙可是帝王之征啊”，抽与层数等量的牌
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        Player? player = card?.Owner;
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        if (Owner is null || !Owner.IsAlive || card is null || player is null || player.Creature != Owner)
        {
            return;
        }

        if (card is not SmilingTiger && card is not DragonOmen)
        {
            return;
        }

        // 抽 Amount 张牌
        await CardPileCmd.Draw(choiceContext, base.Amount, Owner.Player);
    }
}
