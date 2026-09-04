using System.Collections.Generic;
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

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “无限连营”赋予的可叠加能力：层数 = 本回合已打出的“无限连营”张数。
/// 本回合内每打出一张牌，就为原版“下回合抽牌”（DrawCardsNextTurnPower）增加
/// “此前已打出的无限连营张数”层；第 1 张“无限连营”（施放者）此前为 0，故不计入；
/// 第 2 张起每张“无限连营”在打出时按其打出前的张数计入，并令层数 +1。
/// 下回合开始时移除自身（重新清空）。
/// </summary>
[RegisterPower]
public class infinite_camps_power : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 可叠加：Counter 显示当前层数（即本回合已打出的“无限连营”张数）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 出牌与回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 本回合打出的“无限连营”副本（含第 1 张）：它们已在各自的 OnPlay 中
    // 按“打出前已有的张数”计入，这里不再重复处理
    private readonly HashSet<CardModel> _appliedCopies = new();

    public void MarkAppliedBy(CardModel card)
    {
        _appliedCopies.Add(card);
    }

    // 能力图标资源（Counter 层数会显示在图标角标上）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 本回合内每打出一张“无限连营”以外的牌，为原版“下回合抽牌”增加“层数”层
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !Owner.IsAlive)
        {
            return;
        }

        // “无限连营”副本（含第 1 张）已在各自 OnPlay 中按“打出前张数”计入，跳过
        if (_appliedCopies.Contains(cardPlay.Card))
        {
            return;
        }

        // 触发音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/infinite_camps_power");

        // 其他牌：按当前层数（= 本回合已打出的“无限连营”张数）增加下回合抽牌
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner, Amount, Owner, cardPlay.Card);
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
