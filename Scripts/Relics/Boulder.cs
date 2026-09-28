using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Relics;

/// <summary>
/// 巨石（事件遗物）：拾起时，将一张附魔了「注能」的「滚石+」加入你的牌组。
/// （滚石＝原版卡 RollingBoulder，3 费稀有**能力**牌，打出后获得 RollingBoulderPower；
///  升级只提高该能力的层数 5 → 10，见 RollingBoulder.cs:17-36。
///  「注能」＝原版附魔 Imbued：被附魔的牌每场战斗开始时自动打出，且开局排在抽牌堆底
///  （Imbued.cs:11-26）——所以「每场战斗白嫖一张滚石」的效果由附魔承担，遗物本身只在拾起时生效。
///  注意抽牌堆底的排序是引擎按 Enchantment.ShouldStartAtBottomOfDrawPile 处理的，
///  见 CombatManager.cs:660，本 mod 不需要额外代码。）
///
/// 获取方式：只通过事件「实践主义者」——击杀盛碗虫（巨石）后，作为**额外奖励**追加在该场战斗的
/// 标准奖励之上（与「野生中立伏兵」同款，见 Pragmatist.Fight() 里传给
/// EnterCombatWithoutExitingEvent 的 extraRewards）。
/// 因此稀有度用 Event：不参与随机掉落，与本 mod 的「传送门」一致。
/// </summary>
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class Boulder : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    // 事件遗物：不参与随机掉落
    public override RelicRarity Rarity => RelicRarity.Event;

    // 效果只在拾起时生效（顺带让遗物不可交易，与原版拾起类遗物一致，见 RelicModel.cs:184）
    public override bool HasUponPickupEffect => true;

    // 悬停时展示「滚石+」这张牌本身的说明，写法与「卫星城小沛」一致
    // （XiaopeiSatelliteTown.cs:66-68）；升级版用 upgrade: true 显示（同原版「狡诈药剂」
    // CunningPotion.cs:24），因为遗物给的正是升级过的滚石+。
    // 再补一条「注能」附魔说明：附魔文字不在卡牌预览里，照搬原版「放电异虾」
    // （ElectricShrymp.cs:17，它的描述同样提到注能）。
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. HoverTipFactory.FromCardWithCardHoverTips<RollingBoulder>(upgrade: true),
        .. HoverTipFactory.FromEnchantment<Imbued>()
    ];

    // 拾起时：造一张「滚石+」、附魔「注能」、加入牌组。
    // 造牌/入组/预览的写法照搬原版「故事书」（Storybook.cs:17-21）：造牌 → 入组 → 预览返图。
    public override async Task AfterObtained()
    {
        IRunState runState = Owner.RunState;

        // 1) 一张原版「滚石」，升一级得到「滚石+」（RollingBoulderPower 5 → 10）
        CardModel boulder = runState.CreateCard<RollingBoulder>(Owner);
        CardCmd.Upgrade(boulder);

        // 2) 附魔「注能」：之后每场战斗开始时自动打出（含开局沉底）
        ApplyImbued(boulder);

        // 3) 加入牌组。顺序很重要：附魔必须在入组「之前」完成，否则会漏掉那些基于
        //    “入组那一刻”的钩子——例如原版「冰棒」BingBong 会克隆刚入组的牌，克隆体应当也带注能。
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(boulder, PileType.Deck));
    }

    /// <summary>
    /// 给卡牌附魔「注能」（原版 Imbued）。
    ///
    /// 正常情况下走原版接口 <c>CardCmd.Enchant</c>；但原版注能只允许**技能牌**
    /// （Imbued.CanEnchantCardType 仅对 CardType.Skill 返回 true，见 Imbued.cs:15-18），
    /// 而滚石是**能力牌**（RollingBoulder.cs:24），走接口会抛
    /// “Cannot enchant ROLLING_BOULDER with IMBUED.”。
    /// 因此这里退回引擎内部的附魔路径：<c>CardModel.EnchantInternal</c> +
    /// <c>EnchantmentModel.ModifyCard</c> + <c>CardModel.FinalizeUpgradeInternal</c>，
    /// 与 CardCmd.Enchant 的内部实现（CardCmd.cs:532-559）以及存档读档时的附魔恢复路径
    /// （CardModel.cs:2229-2231）完全一致，只是跳过了类型校验。
    ///
    /// 跳过校验是安全的：注能不改任何数值、只挂一个“每场战斗开始时自动打出”的战斗钩子
    /// （EnchantmentModel.ShouldReceiveCombatHooks 取自卡牌本身，而牌在抽牌堆里就是战斗堆，
    /// 见 EnchantmentModel.cs:120 + CardModel.cs:1045），对能力牌同样适用；
    /// <c>CanEnchant</c> 只在“挑选附魔”的场合（附魔选择界面、随机附魔）用作过滤，
    /// 不影响已经附上的附魔，存档也只记 Id + Amount，读档时不会再校验。
    /// </summary>
    private static void ApplyImbued(CardModel card)
    {
        EnchantmentModel imbued = ModelDb.Enchantment<Imbued>().ToMutable();

        if (imbued.CanEnchant(card))
        {
            CardCmd.Enchant(imbued, card, 1m);
            return;
        }

        card.EnchantInternal(imbued, 1m);
        imbued.ModifyCard();
        card.FinalizeUpgradeInternal();
    }
}
