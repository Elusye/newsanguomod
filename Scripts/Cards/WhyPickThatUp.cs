using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WhyPickThatUp : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 自带“奇巧”（Sly）与“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly, CardKeyword.Exhaust];

    // 仅多人模式可用（每人各选的交互在单人下没有意义）
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 从弃牌堆拿回手牌的张数：基础 2，升级后 3
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("ReturnCount", 2m)
    ];

    public WhyPickThatUp() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑：给所有存活玩家附加能力，各自在下个回合开始时从弃牌堆取牌
    // （选择时机放在回合开始而非打出瞬间，否则在“额外回合”中打出会卡死战斗流程）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/why_pick_that_up");

        ICombatState combatState = CombatState!;

        // 所有存活的玩家各获得一层能力，各自在下个回合开始时从自己的弃牌堆中选至多 ReturnCount 张牌
        foreach (Player player in combatState.Players.Where(p => p.Creature is { IsAlive: true }))
        {
            await PowerCmd.Apply<WhyPickThatUpPower>(choiceContext, player.Creature, DynamicVars["ReturnCount"].BaseValue, Owner.Creature, this);
        }
    }

    // 升级：张数 2 → 3
    protected override void OnUpgrade()
    {
        DynamicVars["ReturnCount"].UpgradeValueBy(1m);
    }
}
