using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国（曹魏）专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HailKingOfHanzhong : NewsanguoCardTemplate
{
    // 获得格挡：可被灵巧等格挡附魔识别
    public override bool GainsBlock => true;

    // 2026-10-05：按要求算作天意牌，计入「恨天剑法」的天意牌统计
    // （本牌自身不消耗天意之力，只是“失去天意之力时回手”的天意体系牌）
    public override bool IsHeavensCard => true;

    // 卡图资源（立绘由美术补充：newsanguo/images/cards/HailKingOfHanzhong.png）
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 7 点格挡（升级后 9 点）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move)
    ];

    // 悬停提示：展示“格挡”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block)
    ];

    // 0 费 / 罕见 / 技能 / 目标自身
    public HailKingOfHanzhong() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（音频文件：newsanguo/audios/hail_king_of_hanzhong.mp3|wav|ogg）
        NewsanguoSfx.Play("event:/newsanguo/sfx/hail_king_of_hanzhong");

        // 获得格挡（走 cardPlay 以获得敏捷等格挡加成）
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, fast: false);
    }

    /// <summary>
    /// 每当你失去天意之力时，将这张牌放入你的手牌。
    /// 由 <see cref="newsanguo.Scripts.Combat.HeavensForce"/> 在数值真的减少后统一派发，
    /// 已经握在手上时不重复加入。
    /// </summary>
    public override async Task OnHeavensForceLost(PlayerChoiceContext choiceContext, int amount)
    {
        if (!IsMutable || base.Owner is null || Pile?.Type == PileType.Hand)
        {
            return;
        }

        await CardPileCmd.Add(this, PileType.Hand);
    }

    // 升级：格挡 7 → 9
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }

    // 生成 1 张“参见汉中王！”并加入手牌（供其它卡牌调用，参考原版 Shiv.CreateInHand）
    public static async Task<CardModel?> CreateInHand(Player owner, ICombatState combatState)
    {
        // 战斗已结束或正在结束时不再生成，避免收尾阶段状态错乱
        if (CombatManager.Instance.IsOverOrEnding)
        {
            return null;
        }

        CardModel card = combatState.CreateCard<HailKingOfHanzhong>(owner);
        await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, owner);
        return card;
    }
}
