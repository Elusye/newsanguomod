using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>迭死亡：参照原版 Tender，仅记录持有者出牌带来的本回合敏捷，并在回合结束时还原。</summary>
[RegisterPower]
public class DeadDadPower : ModPowerTemplate
{
    private sealed class Data
    {
        public int dexterityGrantedThisTurn;
    }
    protected override object InitInternalData() => new Data();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    // 与 Tender 相同，图标数字展示本回合累计变化量，而非施加层数。
    public override int DisplayAmount => IsMutable ? GetInternalData<Data>().dexterityGrantedThisTurn : 0;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/DeadDadPower.png",
        BigIconPath: "res://newsanguo/images/powers/DeadDadPowerBig.png");
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Owner.IsAlive || Owner.Player is null || cardPlay.Card.Owner != Owner.Player || Amount <= 0) return;
        int gain = Amount;
        GetInternalData<Data>().dexterityGrantedThisTurn += gain;
        InvokeDisplayAmountChanged();
        Flash();
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, gain, Applier, null, silent: true);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        Data data = GetInternalData<Data>();
        int granted = data.dexterityGrantedThisTurn;
        if (granted > 0)
            await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, -granted, Applier, null, silent: true);
        data.dexterityGrantedThisTurn = 0;
        InvokeDisplayAmountChanged();
    }
}
