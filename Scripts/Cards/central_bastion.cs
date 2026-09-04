using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class central_bastion : NewsanguoCardTemplate
{
    // 基础耗能：2
    private const int energyCost = 2;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 获得格挡：可被灵巧等格挡附魔识别
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：本回合格挡、下回合格挡
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(16m, ValueProp.Move),
        new BlockVar("NextTurnBlock", 8m, ValueProp.Move)
    ];

    // 悬停提示：展示“格挡”的条件说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Block),
    ];

    public central_bastion() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        ICombatState? combatState = base.CombatState;
        if (owner is null || combatState is null)
        {
            return;
        }

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/central_bastion");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 获得本回合格挡（走 cardPlay 以获得敏捷等格挡加成）
        await CreatureCmd.GainBlock(owner.Creature, DynamicVars.Block, cardPlay, fast: false);

        // 下回合格挡：参考原版 Glitterstream，在打出时用 Hook.ModifyBlock 预先计算
        // 修正后的数值（含敏捷、易伤等），存入能力层数；下回合发放时不再二次修正
        BlockVar nextTurnBlockVar = (BlockVar)DynamicVars["NextTurnBlock"];
        decimal nextTurnBlockAmount = Hook.ModifyBlock(
            combatState,
            owner.Creature,
            nextTurnBlockVar.BaseValue,
            nextTurnBlockVar.Props,
            this,
            cardPlay,
            out _);

        // 附加“中原雄关”能力：下回合按层数获得格挡；期间受到未格挡伤害则失去所有层数
        await PowerCmd.Apply<central_bastion_power>(choiceContext, owner.Creature, nextTurnBlockAmount, owner.Creature, this);
    }

    // 升级：本回合格挡 16 → 20，下回合格挡 8 → 10
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars["NextTurnBlock"].UpgradeValueBy(2m);
    }
}
