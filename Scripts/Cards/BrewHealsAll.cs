using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class BrewHealsAll : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 词条：消耗 + 保留
    // 保留（Retain）：回合结束时若未打出就留在手牌里，可以攒到真正需要的那一回合再用
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    // 酒力不足以支付消耗（未升级 ≤5、升级后 ≤3）时红色高亮，提示本次打出不会产生任何效果
    protected override bool ShouldGlowRedInternal => IsUpgraded
        ? Owner.Creature.GetPowerAmount<DrunkenMightPower>() <= 3
        : Owner.Creature.GetPowerAmount<DrunkenMightPower>() <= 5;

    // 卡牌基础数值：消耗的酒力（升级后 4）
    // 用 IntVar（而非 PowerVar<DrunkenMightPower>）：它是“失去”数值，
    // 不应参与 PowerVar 的卡面预览钩子，否则会被“换大盏”错误地加高显示
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("DrunkenMight", 6)
    ];

    // 悬停提示：展示“酒力”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    public BrewHealsAll() :
        base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 酒力不足以支付卡面消耗（未升级 6、升级后 4）则本次打出无效果
        if (Owner.Creature.GetPowerAmount<DrunkenMightPower>() < DynamicVars["DrunkenMight"].IntValue) return;

        // 播放出牌语音
        NewsanguoSfx.Play("event:/newsanguo/sfx/brew_heals_all");

        // 消耗酒力
        await PowerCmd.Apply<DrunkenMightPower>(choiceContext, Owner.Creature, -DynamicVars["DrunkenMight"].BaseValue, Owner.Creature, this);

        // 移除自身的所有负面效果：声明为 Debuff 的能力，以及为负值的可负计数能力
        // （力量、灵巧等 AllowNegative 能力为负时即负面效果）
        // 先快照再逐个移除，避免遍历途中集合变化
        List<PowerModel> debuffs = Owner.Creature.Powers
            .Where(p => p.TypeForCurrentAmount == PowerType.Debuff)
            .ToList();
        foreach (PowerModel debuff in debuffs)
        {
            await PowerCmd.Remove(debuff);
        }

        // 天意之力已改为副资源（不再是可被移除的能力），负值时在此单独归零
        if (HeavensForce.Get(Owner) < 0)
        {
            await HeavensForce.Set(choiceContext, Owner, 0);
        }

        // 消耗自己所有的诅咒牌与状态牌
        // 写法参考原版「火铳齐射」FlakCannon（.decompile/sts2full/sts2.decompiled.cs:140087）：
        // 遍历这场战斗里属于你的全部牌堆（手牌/抽牌堆/弃牌堆/出牌堆），只挑诅咒与状态，已在消耗堆的跳过。
        // 先快照再逐张消耗：消耗本身会移动牌，遍历途中集合会变
        List<CardModel> cursesAndStatuses = Owner.PlayerCombatState.AllCards
            .Where(card => (card.Type == CardType.Curse || card.Type == CardType.Status)
                && card.Pile?.Type != PileType.Exhaust)
            .ToList();
        foreach (CardModel curseOrStatus in cursesAndStatuses)
        {
            await CardCmd.Exhaust(choiceContext, curseOrStatus);
        }

        // 清除自己所有牌上的“标记”（折磨）：折磨不会自动过期，原版由各自能力在回合末自行清理，
        // 因此只移除能力会让标记在本场战斗中永久残留（被标记的牌再也打不出去）
        foreach (CardModel card in Owner.PlayerCombatState.AllCards)
        {
            if (card.Affliction != null)
            {
                CardCmd.ClearAffliction(card);
            }
        }
    }

    // 升级：消耗的酒力 6 → 4
    protected override void OnUpgrade()
    {
        DynamicVars["DrunkenMight"].UpgradeValueBy(-2m);
    }
}
