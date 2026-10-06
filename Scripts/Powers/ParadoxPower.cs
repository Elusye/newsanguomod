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

/// <summary>
/// 「悖论」（Paradox）：被本能力标记的牌本回合不能被打出。
/// 由“博望坡悖论”打出时抽到的牌登记进来（<see cref="LockCards" />），玩家回合结束时能力自动移除，标记随之失效。
/// 实现手法对应原版 HexPower：能力存在期间通过全局关键词钩子给指定牌加 Unplayable，
/// 能力消失后这个来源的关键词自动消失，不需要手工清理，也不改动卡牌自身的 LocalKeywords。
/// 显示名/描述在 localization/*/powers.json 的 NEWSANGUO_POWER_PARADOX_POWER.* 里（名字取自类名）；
/// 能力图标路径按类名取 res://…/powers/ParadoxPower.png（大图 …ParadoxPowerBig.png）。
/// </summary>
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

    // 悬停提示：解释「不能被打出」
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Unplayable)
    ];

    // 登记本回合被锁定的牌（同一回合重复打出时追加；能力实例会被复用，所以集合会累积）
    public void LockCards(IEnumerable<CardModel> cards)
    {
        foreach (CardModel card in cards)
        {
            _lockedCards.Add(card);
        }
    }

    // 能力存在期间，被锁定的牌获得 Unplayable（等价于“不能被打出”）
    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (!_lockedCards.Contains(card))
        {
            return false;
        }

        return keywords.Add(CardKeyword.Unplayable);
    }

    // 玩家回合结束时移除自身（锁定随之失效）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return;
        }

        _lockedCards.Clear();
        await PowerCmd.Remove(this);
    }
}
