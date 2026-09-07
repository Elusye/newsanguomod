using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
/// “曹氏兵法”赋予的可叠加能力：层数（Amount）= 已打出的“曹氏兵法”张数。
/// 1) 你每打出一张牌，就召唤与层数相同数量的奥斯蒂生命值（OstyCmd.Summon，同原版“吸引仇恨”PullAggro）；
/// 2) 每当任何生物死亡，你获得与层数相同的力量（触发点同原版“忧郁”Melancholy 的 AfterDeath Hook）。
/// 打出“曹氏兵法”本卡自身的那次出牌不触发（与“参悟天意”一致）。
/// </summary>
[RegisterPower]
public class CaosArtOfWarPower : ModPowerTemplate
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 可叠加：Counter 显示当前层数（= 已打出的“曹氏兵法”张数），每层提供相同的触发强度
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 出牌与死亡钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 记录最近一次由哪张“曹氏兵法”施加本能力（打出那张牌本身不计入“打出一张牌”）
    private CardModel? _applyingCard;

    public void MarkAppliedBy(CardModel card)
    {
        _applyingCard = card;
    }

    // 能力图标资源（Counter 层数会显示在图标角标上）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 每当你打出一张牌时：召唤与层数相同数量的奥斯蒂生命值
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is null || !Owner.IsAlive)
        {
            return;
        }
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        Player? player = cardPlay.Card?.Owner;
        if (player is null || player.Creature != Owner)
        {
            return;
        }
        // 打出“曹氏兵法”自身时不算“打出的牌”，不触发
        if (cardPlay.Card == _applyingCard)
        {
            return;
        }
        if (Amount <= 0)
        {
            return;
        }

        Flash();
        await OstyCmd.Summon(choiceContext, player, Amount, cardPlay.Card);
    }

    // 每当任何生物死亡时：获得与层数相同的力量
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner is null || !Owner.IsAlive || Amount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }
}
