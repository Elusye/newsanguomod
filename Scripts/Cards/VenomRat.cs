using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Cards;
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class VenomRat : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public override bool IsHeavensCard => true;

    // 失去1点天意之力，给予7/10层中毒
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(1m),
        new PowerVar<PoisonPower>(7m)
    ];

    // 悬停提示：展示“中毒”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<PoisonPower>()
    ];

    // 卡牌自带“保留”和“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust, .. base.CanonicalKeywords];

    public VenomRat() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/venom_rat");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        await HeavensForce.Add(choiceContext, Owner, -DynamicVars["HeavensForcePower"].IntValue, this);

        // 在目标身上播放中毒命中特效
        var child = NPoisonImpactVfx.Create(cardPlay.Target);
        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(child);

        // 给予目标 7（10）层中毒
        await PowerCmd.Apply<PoisonPower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Poison.IntValue,
            base.Owner.Creature,
            this);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 中毒从 7 提高到 10
        DynamicVars.Poison.UpgradeValueBy(3);
    }
}
