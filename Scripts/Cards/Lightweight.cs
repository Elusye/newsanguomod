using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册到状态卡池（模型参照原版 Toxic：1 费、可打出、打出后消耗）
[RegisterCard(typeof(StatusCardPool))]
public class Lightweight : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 关键词：打出后消耗（对应文本由引擎自动追加）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 状态牌不能升级
    public override int MaxUpgradeLevel => 0;

    // 状态牌不参与 modifiers（事件/遗物等）随机生成
    public override bool CanBeGeneratedByModifiers => false;

    // 悬停提示：展示“酒力”能力说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    public Lightweight() : base(1, CardType.Status, CardRarity.Status, TargetType.None)
    {
    }

    // 当酒力要施加到本方身上时，若这张牌此刻在本方手牌中，则将获得量乘以 0（无法再获得酒力）。
    // 负向数值（失去酒力）不受影响；不损失酒力的“消耗”路径也照常。
    //
    // 用 ModifyPowerAmountGivenMultiplicative（而非 TryModifyPowerAmountReceived）：
    // 原版 PowerVar<T>.UpdateCardPreview 只调用 ModifyPowerAmountGiven（= 本钩子所在的 Given 侧），
    // 因此手牌里的酒力数值会自动跟着显示为 0（不打这个钩子的话卡面仍显示原值）。
    // 用“乘 0”而非“减去原值”：换大盏等加成同样挂在 Given 侧，加法会被抵消掉，乘 0 才能保持“无法再获得”的语义。
    public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        // 只拦截施加给本方（持有者）的“酒力”正向获得
        if (power is not DrunkenMightPower) return 1m;
        if (amount <= 0m) return 1m;

        // 这张牌不在本方手牌中则不拦截（抽牌堆/弃牌堆/消耗堆均放行）
        if (base.Pile?.Type != PileType.Hand) return 1m;

        Creature? owner = base.Owner?.Creature;
        if (owner is null) return 1m;

        // 判定“这次是不是给本方加酒力”。原版 Hook.ModifyPowerAmountGiven 会遍历全场所有模型
        // （对方手牌里的这张牌同样会被问到），所以这里必须把归属判干净：
        //  · target 明确指定时以 target 为准：是别人 → 放行；是本方 → 拦截（含盟友“痛饮庆功酒”转给本方的酒力）。
        //  · target 为空时（原版 PowerVar.UpdateCardPreview 的卡面预览路径就传 null）用 giver 兜底：
        //    只有 giver 也是本方（= 正在预览本方的牌）才拦截。
        // 旧写法把“target 为空”一律当成自己，于是 A 手牌里的不胜酒力会把 B 的酒力获得也乘 0
        // （联机实测：A 手牌有不胜酒力时，B 无法获得酒力）。与 ToABiggerGobletPower.ShouldBoost 的 giver 校验一致。
        if (target is not null)
        {
            return target == owner ? 0m : 1m;
        }
        return giver == owner ? 0m : 1m;
    }

    // 当这张牌在本方手牌中时，拦截本方所有攻击牌的打出（卡面置灰，UnplayableReason.BlockedByHook）。
    // 卡牌自身也参与 Hook.ShouldPlay 的遍历，写法参考原版诅咒 Normality：
    // 先判归属、再判自己是否在手牌，最后只拦攻击牌；牌不在手牌时一律放行。
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != base.Owner) return true;
        if (base.Pile?.Type != PileType.Hand) return true;
        return card.Type != CardType.Attack;
    }

    // 生成 1 张“不胜酒力”并加入弃牌堆（供其它卡牌调用，参考原版 Shiv.CreateInHand）
    public static async Task<CardModel?> CreateInDiscard(Player owner, ICombatState combatState)
    {
        // 战斗已结束或正在结束时不再生成，避免收尾阶段状态错乱
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return null;
        }

        CardModel lightweight = combatState.CreateCard<Lightweight>(owner);
        await CardPileCmd.AddGeneratedCardsToCombat([lightweight], PileType.Discard, owner);
        return lightweight;
    }
}
