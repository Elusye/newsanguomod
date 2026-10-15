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
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>悖论：被标记的牌本回合耗能增加1，回合结束时移除。</summary>
[RegisterPower]
public class ParadoxPower : ModPowerTemplate
{
    // 本回合被锁定的牌（按引用比较；CardModel 未重写 Equals）
    private readonly HashSet<CardModel> _lockedCards = [];

    // 负面效果：Debuff
    public override PowerType Type => PowerType.Debuff;
    // 不叠加：存在即生效
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要战斗钩子（全局关键词钩子 + 回合结束钩子）
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 登记本回合加费的牌（同一回合重复打出时追加；能力实例会被复用，所以集合会累积）
    public void LockCards(IEnumerable<CardModel> cards)
    {
        foreach (CardModel card in cards)
        {
            _lockedCards.Add(card);
            card.InvokeEnergyCostChanged();
        }
    }

    // 仅在本能力存在期间加费，不永久修改卡牌费用。
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (!_lockedCards.Contains(card) || originalCost < 0) return false;
        modifiedCost = originalCost + 1m;
        return true;
    }
    // 玩家回合结束时移除自身（锁定随之失效）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return;
        }

        var marked = _lockedCards.ToArray();
        _lockedCards.Clear();
        await PowerCmd.Remove(this);
        foreach (var card in marked) card.InvokeEnergyCostChanged();
    }
}
