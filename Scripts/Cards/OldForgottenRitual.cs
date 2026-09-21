using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 被遗忘的仪式（旧）：旧版本的被遗忘的仪式，若本回合消耗过卡牌则获得能量
// 写法照搬原版 ForgottenRitual，唯一例外：**不加"消耗"关键词**（Old 系列要求），
// 因此卡面不会出现引擎按 CardKeywordOrder.afterDescription 自动追加的"消耗"那一行，
// 改为把它放进悬停提示里解释。
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class OldForgottenRitual : NewsanguoCardTemplate
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<IroncladCardPool>();

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的能量
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(3)
    ];

    // 悬停提示：能量说明 + “消耗”关键词说明（照搬原版 ForgottenRitual 的 ExtraHoverTips）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        base.EnergyHoverTip,
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    // 特效资源在跑图时预载（照搬原版 ForgottenRitual 的 ExtraRunAssetPaths）
    protected override IEnumerable<string> ExtraRunAssetPaths => NGroundFireVfx.AssetPaths;

    // 本回合是否消耗过卡牌时金色高亮（提示会获得能量）
    protected override bool ShouldGlowGoldInternal => WasCardExhaustedThisTurn;

    // 本回合是否消耗过卡牌。照搬原版：只统计**自己**消耗的牌（e.Card.Owner == base.Owner），
    // 多人下队友消耗牌不会点亮/触发这张牌。
    // 与原版的唯一差别：多一层 CombatState 空值防护（原版直接传 base.CombatState）。
    private bool WasCardExhaustedThisTurn =>
        base.CombatState is { } combatState
        && CombatManager.Instance.History.Entries
            .OfType<CardExhaustedEntry>()
            .Any(e => e.HappenedThisTurn(combatState) && e.Card.Owner == base.Owner);

    public OldForgottenRitual() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑（照搬原版 ForgottenRitual：地面紫火特效 → 施法动画 → 获得能量）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (WasCardExhaustedThisTurn)
        {
            // 与原版写成局部变量不同：Create 返回可空（NGroundFireVfx?），
            // 直接传给 AddChildSafely(this Node, Node?) 就行，也避免 CS8600 警告
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(base.Owner.Creature, VfxColor.Purple));
            await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, base.Owner);
        }
    }

    // 升级：获得的能量 3 → 4
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
