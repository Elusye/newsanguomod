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
public class Intoxicated : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 2 点酒力；若上一张打出的是技能牌，额外获得 2 点酒力
    // 额外部分用 IntVar：实际结算时两部分合并为一次酒力获得，“换大盏”只加成一次，
    // 若这里也用 PowerVar，卡面会把它算成两次加成而显示偏大
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>(2m),
        new IntVar("IntoxicatedBonus", 2)
    ];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    // 上一张打出的牌是技能牌时金色高亮（提示会获得额外酒力）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (base.CombatState is null)
            {
                return false;
            }
            CardPlayFinishedEntry? lastPlay = CombatManager.Instance.History.CardPlaysFinished
                .LastOrDefault(entry => entry.CardPlay?.Card?.Owner == base.Owner);
            return lastPlay is not null && lastPlay.CardPlay.Card.Type == CardType.Skill;
        }
    }

    public Intoxicated() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/intoxicated");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 本场战斗中打出的上一张牌（此牌尚未结算完成，不会把自己算进去）
        CardPlayFinishedEntry? lastPlay = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry => entry.CardPlay?.Card?.Owner == base.Owner);
        bool lastWasSkill = lastPlay is not null && lastPlay.CardPlay.Card.Type == CardType.Skill;

        int wineAmount = DynamicVars["DrunkenMightPower"].IntValue;
        if (lastWasSkill)
        {
            wineAmount += DynamicVars["IntoxicatedBonus"].IntValue;
        }

        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext,
            base.Owner.Creature,
            wineAmount,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 基础酒力 2 → 3
        DynamicVars["DrunkenMightPower"].UpgradeValueBy(1);
        // 额外酒力 2 → 3
        DynamicVars["IntoxicatedBonus"].UpgradeValueBy(1);
    }
}
