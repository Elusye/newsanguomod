using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class tremble : NewsanguoCardTemplate
{
    // 基础耗能：0
    private const int energyCost = 0;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：所有敌人
    private const TargetType targetType = TargetType.AllEnemies;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：对所有敌人施加的虚弱与易伤层数
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>("WeakPower", 1),
        new PowerVar<VulnerablePower>("VulnerablePower", 1)
    ];

    // 固有 + 消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];

    // 悬停提示：展示“虚弱”和“易伤”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    public tremble() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        var combatState = CombatState;
        if (combatState is null)
        {
            return;
        }

        // 播放出牌音效
        SfxCmd.Play("event:/newsanguo/sfx/tremble");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 给予所有敌人虚弱和易伤
        int weak = DynamicVars["WeakPower"].IntValue;
        int vulnerable = DynamicVars["VulnerablePower"].IntValue;
        foreach (Creature enemy in combatState.GetOpponentsOf(owner.Creature).Where(c => c.IsAlive))
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, weak, owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, vulnerable, owner.Creature, this);
        }
    }

    // 升级：虚弱与易伤 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars["WeakPower"].UpgradeValueBy(1m);
        DynamicVars["VulnerablePower"].UpgradeValueBy(1m);
    }
}
