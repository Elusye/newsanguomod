using System.Collections.Generic;
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
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class unstoppable : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：技能
    private const CardType type = CardType.Skill;
    // 卡牌稀有度：稀有
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：失去的天意之力、获得的无实体层数（变量用正值，打出时取负）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<heavens_force>("heavens_force", 5),
        new DynamicVar("IntangibleAmount", 1m)
    ];

    // 悬停提示：展示“无实体”、“天意之力”与“天意侵蚀”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<heavens_force>(),
        HoverTipFactory.FromPower<heavens_decay_power>()
    ];

    // 自带“虚无”关键词（升级后移除）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    public unstoppable() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
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

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/unstoppable");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 获得 1 层无实体
        await PowerCmd.Apply<IntangiblePower>(choiceContext, owner.Creature, DynamicVars["IntangibleAmount"].IntValue, owner.Creature, this);

        // 失去 6 点天意之力
        int lostAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<heavens_force>(choiceContext, owner.Creature, -lostAmount, owner.Creature, this, silent: false);
    }

    // 升级：移除“虚无”，失去的天意之力 5 → 4
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
    }

    // 降级：恢复“虚无”
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Ethereal);
    }
}
