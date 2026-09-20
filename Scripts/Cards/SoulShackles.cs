using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 多人牌：注册到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class SoulShackles : NewsanguoCardTemplate
{
    // 仅多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：失去的天意之力
    // 用 IntVar（而非 HeavensForceVar）：它是“失去”数值，不应参与卡面数值预览钩子
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("HeavensLost", 3)
    ];

    // 悬停提示：展示“天意之力”与“连环”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<ChainPower>()
    ];

    // 属于“天意”体系（涉及天意之力）
    public override bool IsHeavensCard => true;

    public SoulShackles() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑：失去 3 点天意之力，然后所有存活玩家获得“连环”
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/soul_shackles");

        // 失去 3 点天意之力（天意之力允许负值）
        await HeavensForce.Add(choiceContext, base.Owner, -DynamicVars["HeavensLost"].IntValue, this);

        // 所有存活的玩家各获得一层“连环”（本人也在内）
        ICombatState combatState = CombatState!;
        foreach (Player player in combatState.Players.Where(p => p.Creature is { IsAlive: true }))
        {
            await PowerCmd.Apply<ChainPower>(choiceContext, player.Creature, 1, base.Owner.Creature, this);
        }
    }

    // 升级：失去的天意之力 3 → 2
    protected override void OnUpgrade()
    {
        DynamicVars["HeavensLost"].UpgradeValueBy(-1);
    }
}
