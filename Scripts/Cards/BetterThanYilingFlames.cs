using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class BetterThanYilingFlames : NewsanguoCardTemplate
{
    // 基础伤害。永久成长在此之上累加，本体数值不变。
    private const int BaseDamage = 6;

    // 当前伤害与累计的永久成长值。
    // 参照原版「基因算法」（GeneticAlgorithm.cs:22-57）：两者都用 [SavedProperty] 跟随牌库本体存档，
    // 跨战斗保留；且属性 setter 里同步 DynamicVars，这样读档/降级后引擎回填属性时卡面数值立刻跟上。
    private int _currentDamage = BaseDamage;
    private int _increasedDamage;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 CurrentDamage 点伤害；每消耗一张手牌永久增加 Increase 点（升级后 2）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(CurrentDamage, ValueProp.Move),
        new DynamicVar("Increase", 1m)
    ];

    public BetterThanYilingFlames() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 本牌当前的伤害值（= 基础 6 + 永久成长）
    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            DynamicVars.Damage.BaseValue = _currentDamage;
        }
    }

    // 累计的永久成长值
    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
        }
    }

    // 打出时的效果逻辑：造成伤害 → 消耗任意张手牌（玩家自己选，可不选） → 每消耗一张牌永久 +Increase 点伤害
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/better_than_yiling_flames");

        // 在目标脚下燃起火焰特效
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(cardPlay.Target));

        // 1) 先造成当前伤害：本次攻击的数值在此刻已经固定，
        //    下面消费手牌换来的成长只影响以后的打出（与卡面文案顺序一致）。
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 2) 从手牌中选择任意张牌消耗掉（最少 0 张，也就是可以不消耗）。
        //    先取一份选中列表再逐张消耗：一边遍历手牌一边把牌移出牌堆会在迭代中漏牌。
        int exhaustedCount = 0;
        CardPile? hand = PileType.Hand.GetPile(base.Owner);
        if (hand is not null && hand.Cards.Count > 0)
        {
            List<CardModel> selectedList = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: base.Owner,
                prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_ANY"), 0, hand.Cards.Count),
                filter: null,
                source: this)).ToList();

            foreach (CardModel card in selectedList)
            {
                await CardCmd.Exhaust(choiceContext, card);

                // 只有真的进了消耗堆才计数：被钩子拦下 / 未成功移动的牌不算“消耗”
                if (card.Pile?.Type == PileType.Exhaust)
                {
                    exhaustedCount++;
                }
            }
        }

        // 3) 每消耗一张牌，本牌伤害永久增加（自身 + 牌库本体，跨战斗保留）
        if (exhaustedCount > 0)
        {
            int extraDamage = DynamicVars["Increase"].IntValue * exhaustedCount;
            BuffFromPlay(extraDamage);

            // 牌库本体：升级/存档都记在它身上。复制品（CreateClone）的 DeckVersion 会被引擎清空，
            // 因此复制品打出的成长只在本场战斗内有效——与原版「基因算法」一致。
            if (base.DeckVersion is BetterThanYilingFlames deckCopy && !ReferenceEquals(deckCopy, this))
            {
                deckCopy.BuffFromPlay(extraDamage);
            }
        }
    }

    // 升级：每消耗一张牌的永久成长量 1 → 2（基础伤害不变）
    protected override void OnUpgrade()
    {
        base.DynamicVars["Increase"].UpgradeValueBy(1m);
    }

    // 降级/重建后 DynamicVars 会被重置，按“基础伤害 + 永久成长”重新同步
    protected override void AfterDowngraded()
    {
        CurrentDamage = BaseDamage + IncreasedDamage;
    }

    // 卡面附加参数：告诉文案“本牌是不是复制品”。
    // 复制品的 DeckVersion 会被引擎清空（CardModel.AfterCloned，sts2.decompiled.cs:74119-74140），
    // 用它打出只会让本场这张副本变强，不会写回牌库本体——卡面上要说清楚。
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        AddIsCloneDescriptionArg(description);
    }

    // 增加永久成长并同步当前伤害值
    private void BuffFromPlay(int extraDamage)
    {
        IncreasedDamage += extraDamage;
        CurrentDamage = BaseDamage + IncreasedDamage;
    }
}
