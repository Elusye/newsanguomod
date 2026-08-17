using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “无限连营”赋予的标记能力：本回合每打出一张牌（不含本卡自身），
/// 就为原版“下回合抽牌”（DrawCardsNextTurnPower）增加一层；下回合开始时移除自身。
/// </summary>
[RegisterPower]
public class infinite_camps_power : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 仅作为激活标记，不参与层数显示
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    // 出牌与回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 打出“无限连营”本卡自身时不计入层数
    private CardModel? _applyingCard;

    public void MarkAppliedBy(CardModel card)
    {
        _applyingCard = card;
    }

    // 能力图标资源（隐藏能力不显示，保留以兼容资源加载）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 本回合内每打出一张牌，为原版“下回合抽牌”直接增加一层
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !Owner.IsAlive)
        {
            return;
        }

        // 打出“无限连营”自身时不增加层数
        if (cardPlay.Card == _applyingCard)
        {
            return;
        }

        // 触发音效：本回合每打出一张牌计一层
        SfxCmd.Play("event:/newsanguo/sfx/infinite_camps_power");

        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner, 1, Owner, cardPlay.Card);
    }

    // 下回合开始时：移除本标记能力（原版“下回合抽牌”会在本轮抽牌后自动移除）
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner)
        {
            return;
        }

        await PowerCmd.Remove(this);
    }
}
