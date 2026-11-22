using newsanguo.Scripts.Settings;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class TigerWindCloudDragonPower : ModPowerTemplate
{

    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示额外重放次数（每打出一次“风从虎，云从龙”叠加）
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

    private sealed class DrawData
    {
        public readonly Dictionary<CardModel, int> DrawCounts = new();
    }

    protected override object InitInternalData() => new DrawData();

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner.IsAlive &&
            cardPlay.Card is NewsanguoCardTemplate { IsTigerDragonCard: true } card &&
            card.Owner.Creature == Owner)
        {
            GetInternalData<DrawData>().DrawCounts[card] = Amount;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner.IsAlive &&
            GetInternalData<DrawData>().DrawCounts.Remove(play.Card, out int amount) && amount > 0)
        {
            await CardPileCmd.Draw(context, amount, play.Card.Owner);
        }
    }

}
