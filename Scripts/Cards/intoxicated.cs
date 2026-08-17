using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class intoxicated : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：普通
    private const CardRarity rarity = CardRarity.Common;
    // 目标类型（Self 表示对自己/玩家）
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 2 点酒力；若上一张打出的是技能牌，额外获得 2 点酒力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<drunken_might>("drunken_might", 2),
        new PowerVar<drunken_might>("intoxicated_bonus", 2)
    ];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<drunken_might>()
    ];

    public intoxicated() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        SfxCmd.Play("event:/newsanguo/sfx/intoxicated");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 本场战斗中打出的上一张牌（此牌尚未结算完成，不会把自己算进去）
        CardPlayFinishedEntry? lastPlay = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry => entry.CardPlay?.Card?.Owner == owner);
        bool lastWasSkill = lastPlay is not null && lastPlay.CardPlay.Card.Type == CardType.Skill;

        int wineAmount = DynamicVars["drunken_might"].IntValue;
        if (lastWasSkill)
        {
            wineAmount += DynamicVars["intoxicated_bonus"].IntValue;
        }

        await PowerCmd.Apply<drunken_might>(
            choiceContext,
            owner.Creature,
            wineAmount,
            owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 基础酒力 2 → 3
        DynamicVars["drunken_might"].UpgradeValueBy(1);
        // 额外酒力 2 → 3
        DynamicVars["intoxicated_bonus"].UpgradeValueBy(1);
    }
}
