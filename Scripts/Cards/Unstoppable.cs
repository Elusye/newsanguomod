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
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class Unstoppable : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：失去的天意之力、获得的无实体层数（变量用正值，打出时取负）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(5m),
        new PowerVar<IntangiblePower>(1m)
    ];

    // 悬停提示：展示“无实体”、“天意之力”与“天意侵蚀”的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<IntangiblePower>(),
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 自带“虚无”关键词（升级后移除）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public Unstoppable() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/unstoppable");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得 1 层无实体
        await PowerCmd.Apply<IntangiblePower>(choiceContext, base.Owner.Creature, DynamicVars["IntangiblePower"].IntValue, base.Owner.Creature, this);

        // 失去 6 点天意之力
        int lostAmount = DynamicVars["HeavensForcePower"].IntValue;
        await HeavensForce.Add(choiceContext, base.Owner, -lostAmount, this);
    }

    // 升级：失去的天意之力 5 → 4（虚无关键词升级后保留）
    protected override void OnUpgrade()
    {
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
    }
}
