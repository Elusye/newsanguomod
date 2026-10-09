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

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 2026-10-08：按要求从「曹魏」（新三国）卡池移除，改为只注册在蜀汉卡池
// （同时确认未出现在 NewsanguoCardPool.CardTypes 中，避免仍然命中曹魏池）
[RegisterCard(typeof(ShuHanCardPool))]
public class WhyPickThatUp : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 自带“奇巧”（Sly）关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Sly];

    // 2026-10-08：按要求改为单人也能遇到这张牌。
    // 原实现是 MultiplayerOnly（理由是“每人各选”的交互在单人下没意义），
    // 但效果本身对单人完全成立（给所有存活玩家各挂一层能力，单人即只有自己一层），
    // 因此这里不再重写 MultiplayerConstraint，回到基类默认值 None（单人/多人都会出现）。
    // 相关：能力 WhyPickThatUpPower.BeforeHandDraw 已按玩家逐个处理，且音效只在 LocalContext.IsMe 时播放。

    // 从弃牌堆拿回手牌的张数：基础 2，升级后 3
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("ReturnCount", 2m)
    ];

    public WhyPickThatUp() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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
