using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public sealed class FollowHeavensWill : NewsanguoCardTemplate
{
    public override bool IsHeavensCard => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/FollowHeavensWill.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FollowHeavensWillPower>(1m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HeavensForce.HoverTip()];

    public FollowHeavensWill() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/follow_heavens_will");
        await PowerCmd.Apply<FollowHeavensWillPower>(choiceContext, Owner.Creature,
            DynamicVars["FollowHeavensWillPower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);

    protected override void AfterDowngraded() => RemoveKeyword(CardKeyword.Innate);
}
