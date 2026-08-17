using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “参悟天意”：你每打出一张牌，获得与层数相等的天意之力。可叠加。
/// </summary>
[RegisterPower]
public class divine_insight_power : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，层数 = 每打出一张牌获得的天意之力（多次打出叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 出牌钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 打出“参悟天意”本卡自身时不触发
    private CardModel? _applyingCard;

    public void MarkAppliedBy(CardModel card)
    {
        _applyingCard = card;
    }

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 打出任意牌后：获得与层数相等（Amount）的天意之力
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is null)
        {
            return;
        }
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        if (cardPlay.Card?.Owner?.Creature != Owner)
        {
            return;
        }
        // 打出“参悟天意”自身时不算“打出的牌”，不触发
        if (cardPlay.Card == _applyingCard)
        {
            return;
        }

        // 触发“参悟天意”音效（对应 FMOD 事件 event:/newsanguo/sfx/divine_insight_power）
        SfxCmd.Play("event:/newsanguo/sfx/divine_insight_power");
        
        await PowerCmd.Apply<heavens_force>(choiceContext, Owner, Amount, Owner, cardPlay.Card, silent: false);
    }
}
