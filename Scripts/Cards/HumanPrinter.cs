using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 2026-10-05：蜀汉专属卡（只注册在蜀汉卡池）
[RegisterCard(typeof(ShuHanCardPool))]
public class HumanPrinter : NewsanguoCardTemplate
{
    // 卡图资源（立绘由美术补充：newsanguo/images/cards/HumanPrinter.png）
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：复制 5 张（升级只改费用，张数不变）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new RepeatVar(5)
    ];

    // 升级前后都自带“消耗”关键词（合并 base 以保留模板附加的模组关键词）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 1 费 / 稀有 / 技能 / 目标自身
    public HumanPrinter() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（音频文件：newsanguo/audios/human_printer.mp3|wav|ogg）
        NewsanguoSfx.Play("event:/newsanguo/sfx/human_printer");

        // 从手牌中选择一张牌（手牌为空时直接结束）
        CardModel? chosen = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext,
            player: base.Owner,
            filter: null,
            source: this)).FirstOrDefault();

        if (chosen is null)
        {
            return;
        }

        // 复制品保留所选牌当前的升级/附魔状态（原版「双重施法」DualWield、「传家宝锤」HeirloomHammer 同款写法）
        List<CardModel> copies = [];
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            copies.Add(chosen.CreateClone());
        }

        // 添加进抽牌堆：位置照原版 Dirge / 本 mod「挽歌」为 Random（洗入抽牌堆）
        var result = await CardPileCmd.AddGeneratedCardsToCombat(
            copies, PileType.Draw, base.Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(result);
    }

    // 升级：费用 1 → 0（复制张数不变，消耗关键词升级前后都有）
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
