using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 被遗忘的仪式（旧）：旧版本的被遗忘的仪式，若本回合消耗过卡牌则获得能量
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

    // 悬停提示：展示“消耗”关键词的说明（卡牌以本回合是否消耗过牌为条件，自身不消耗）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    // 本回合消耗过卡牌时金色高亮（提示会获得能量）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            ICombatState? combatState = base.CombatState;
            if (combatState is null)
            {
                return false;
            }
            return CombatManager.Instance.History.Entries
                .OfType<CardExhaustedEntry>()
                .Any(entry => entry.HappenedThisTurn(combatState));
        }
    }

    public OldForgottenRitual() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 若本回合消耗过卡牌，则获得能量
        bool wasExhausted = CombatManager.Instance.History.Entries
            .OfType<CardExhaustedEntry>()
            .Any(entry => entry.HappenedThisTurn(combatState));
        if (wasExhausted)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, base.Owner);
        }
    }

    // 升级：获得的能量 3 → 4
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}
