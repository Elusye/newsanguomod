using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

/// <summary>
/// 三顾茅庐（任意一幕事件）：分三次拜访，越往后奖励越好。
/// 前置节点：只有「来到茅庐前」一个选项，点击后进入一顾的剧情页（首屏文字过长，故拆成两屏）。
/// 第一阶段：获得 30 金币；或失去 4 点生命进入下一阶段。
/// 第二阶段：获得 70 金币；或失去 4 点生命进入下一阶段。
/// 第三阶段：从牌组中移除 2 张牌；或变化 3 张牌，并将一张诅咒「悔恨」加入牌组。
/// </summary>
// 任意一幕均可遇到：第一幕可能是 Underdocks 或 Overgrowth，第二幕 Hive，第三幕 Glory
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public class ThreeVisitsToTheThatchedCottage : ModEventTemplate
{
    // 页面名（对应本地化键里的 pages.<页面名>）
    private const string InitialPage = "INITIAL";
    private const string FirstVisitPage = "FIRST_VISIT";
    private const string SecondVisitPage = "SECOND_VISIT";
    private const string ThirdVisitPage = "THIRD_VISIT";

    // 当前阶段：0 = 一顾，1 = 二顾，2 = 三顾
    private int _visit;

    private int Visit
    {
        get => _visit;
        set
        {
            AssertMutable();
            _visit = value;
        }
    }

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"res://newsanguo/images/events/{GetType().Name}.png"
    );

    // 选项描述里的 {FirstGold} / {SecondGold} / {HpLoss} / {RemoveCount} / {TransformCount}
    // 都引用这里的数值，改数值只需改这一处
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new GoldVar("FirstGold", 30),
        new GoldVar("SecondGold", 70),
        new HpLossVar(4m),
        new CardsVar("RemoveCount", 2),
        new CardsVar("TransformCount", 3)
    ];

    // 首屏（前置节点）：只有「来到茅庐前」一个选项
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, ArriveAtCottage, ModOptionKey(InitialPage, "ARRIVE"))
        ];
    }

    // 进入茅庐，展示一顾的剧情与选项
    private Task ArriveAtCottage()
    {
        SetEventState(PageDescription(FirstVisitPage),
        [
            new EventOption(this, TakeThirtyGold, ModOptionKey(FirstVisitPage, "TAKE_GOLD")),
            new EventOption(this, VisitAgain, ModOptionKey(FirstVisitPage, "VISIT_AGAIN"))
        ]);
        return Task.CompletedTask;
    }

    // 拿到第一阶段的 30 金币，事件结束
    private async Task TakeThirtyGold()
    {
        await PlayerCmd.GainGold(DynamicVars["FirstGold"].IntValue, Owner!);
        SetEventFinished(PageDescription("TAKE_GOLD_30"));
    }

    // 拿到第二阶段的 70 金币，事件结束
    private async Task TakeSeventyGold()
    {
        await PlayerCmd.GainGold(DynamicVars["SecondGold"].IntValue, Owner!);
        SetEventFinished(PageDescription("TAKE_GOLD_70"));
    }

    // 失去生命继续拜访：一顾后进二顾页，二顾后进三顾页
    private async Task VisitAgain()
    {
        await LoseHp(DynamicVars["HpLoss"].BaseValue);
        Visit++;
        if (Visit < 2)
        {
            SetEventState(PageDescription(SecondVisitPage),
            [
                new EventOption(this, TakeSeventyGold, ModOptionKey(SecondVisitPage, "TAKE_GOLD")),
                new EventOption(this, VisitAgain, ModOptionKey(SecondVisitPage, "VISIT_AGAIN"))
            ]);
            return;
        }
        SetEventState(PageDescription(ThirdVisitPage),
        [
            new EventOption(this, RemoveTwoCards, ModOptionKey(ThirdVisitPage, "REMOVE_TWO_CARDS")),
            new EventOption(this, TransformThreeAndRegret, ModOptionKey(ThirdVisitPage, "TRANSFORM_THREE"),
                HoverTipFactory.FromCardWithCardHoverTips<Regret>())
        ]);
    }

    // 三顾之选一：从牌组中移除 2 张牌
    private async Task RemoveTwoCards()
    {
        List<CardModel> cards = (await CardSelectCmd.FromDeckForRemoval(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, DynamicVars["RemoveCount"].IntValue),
            player: Owner!)).ToList();
        await CardPileCmd.RemoveFromDeck(cards);
        SetEventFinished(PageDescription("REMOVE_TWO_CARDS"));
    }

    // 三顾之选二：变化 3 张牌，并将一张诅咒「悔恨」加入牌组
    private async Task TransformThreeAndRegret()
    {
        List<CardModel> cards = (await CardSelectCmd.FromDeckForTransformation(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, DynamicVars["TransformCount"].IntValue),
            player: Owner!)).ToList();
        foreach (CardModel card in cards)
        {
            await CardCmd.TransformToRandom(card, Rng, CardPreviewStyle.EventLayout);
        }
        await CardPileCmd.AddCurseToDeck<Regret>(Owner!);
        SetEventFinished(PageDescription("TRANSFORM_THREE"));
    }

    // 固定的生命损失：不可格挡、不受力量等增伤修饰，也不归属任何卡牌
    private async Task LoseHp(decimal amount)
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner!.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }
}
