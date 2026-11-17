using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class WinThePeoplesHearts : NewsanguoCardTemplate
{
    public override bool IsHeavensCard => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/WinThePeoplesHearts.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HeavensForceVar(1m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HeavensForce.HoverTip()];

    public WinThePeoplesHearts() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/win_the_peoples_hearts");
        int handCount = Owner.PlayerCombatState?.Hand.Cards.Count ?? 0;
        return HeavensForce.Gain(choiceContext, Owner,
            handCount * DynamicVars["HeavensForcePower"].IntValue, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);

    protected override void AfterDowngraded() => RemoveKeyword(CardKeyword.Retain);
}
