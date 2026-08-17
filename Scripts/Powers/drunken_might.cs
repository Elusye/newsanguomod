using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

// 注册能力到游戏
[RegisterPower]
public class drunken_might : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示层数（和力量一致）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 酒力不会为负数
    public override bool AllowNegative => false;
    // 允许接收战斗钩子，否则 AfterCardPlayed 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 增加攻击牌造成的伤害（返回要叠加的数值增量）
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 只有这张能力的拥有者打出的攻击牌才享受加成
        if (base.Owner != dealer)
        {
            return 0m;
        }
        if (!props.IsPoweredAttack())
        {
            return 0m;
        }
        return base.Amount;
    }

    // 酒力减半（向下取整）：打出攻击牌后消耗一半酒力
    public async Task HalfForCard(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        if (Owner is null) return;

        int newAmount = Amount / 2;
        if (newAmount != Amount)
        {
            await PowerCmd.ModifyAmount(
                choiceContext,
                this,
                newAmount - Amount,
                Owner,
                cardSource,
                silent: false);
        }
    }

    // 打出攻击牌后，酒力层数减半（向下取整）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is null) return;
        if (cardPlay.Card?.Type != CardType.Attack) return;
        if (cardPlay.Card?.Owner?.Creature != Owner) return;

        // 温酒斩击在攻击后获得酒力，减半在其 OnPlay 内手动完成（先减半再获得），此处跳过
        if (cardPlay.Card is wine_cut)
        {
            return;
        }

        await HalfForCard(choiceContext, cardPlay.Card);
    }
}
