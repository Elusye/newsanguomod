using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class OneManStand : NewsanguoCardTemplate
{

    // 获得格挡：可被灵巧等格挡附魔识别
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 13 点格挡（升级 16）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(13m, ValueProp.Move)
    ];

    // 鼠标悬停时展示格挡说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Block)];

    public OneManStand() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/one_man_stand");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, fast: false);

        // 丢弃手牌到只剩一张：选择一张保留，其余全部丢弃
        CardPile hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count <= 1)
        {
            return;
        }

        CardModel? keepCard = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", "NEWSANGUO_CARD_SELECT_ONE_TO_KEEP"), 1),
            context: choiceContext,
            player: base.Owner,
            filter: null,
            source: this)).FirstOrDefault();
        if (keepCard is null)
        {
            return;
        }

        // 弃置其余手牌：必须走 CardCmd.Discard 且一次传入全部（勿循环单张调 Discard），
        // 引擎才会在其中检查并触发奇巧（Sly）——奇巧牌被弃时会自动免费打出而非进弃牌堆；
        // 若直接用 CardPileCmd.Add 逐张移到弃牌堆会绕过 Sly 检查，导致奇巧不触发。
        await CardCmd.Discard(choiceContext, hand.Cards.Where(c => c != keepCard).ToList());
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 格挡从 13 提高到 16
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
