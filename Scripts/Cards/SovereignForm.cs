using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Interop;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class SovereignForm : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HeavensForceVar(1m)];

    // 悬停提示：酒力、被加入手牌的「参见汉中王！」，以及天意之力资源说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..SunqianUniverseVigorInterop.ActiveResourceHoverTips(this),
        ..SunqianUniverseVigorInterop.InteractionHoverTips(gainsResource: true),
        HoverTipFactory.FromCard<HailKingOfHanzhong>(),
        HeavensForce.HoverTip()
    ];

    // 属于“天意”体系（涉及天意之力）
    public override bool IsHeavensCard => true;

    public SovereignForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        SunqianUniverseVigorInterop.AddActiveResourceName(description, this);
    }

    // 打出时的效果逻辑：挂上能力，再把一张「参见汉中王！」塞进手牌
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（音频文件：newsanguo/audios/sovereign_form.mp3|wav|ogg）
        NewsanguoSfx.Play("event:/newsanguo/sfx/sovereign_form");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 施加能力：层数 = 每次触发放给的天意之力
        await PowerCmd.Apply<SovereignFormPower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["HeavensForcePower"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);

        // 将一张「参见汉中王！」加入手牌
        await HailKingOfHanzhong.CreateInHand(Owner, CombatState!);
    }

    protected override void OnUpgrade() => DynamicVars["HeavensForcePower"].UpgradeValueBy(1m);
}
