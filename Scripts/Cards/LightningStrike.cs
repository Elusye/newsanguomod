using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

[RegisterCard(typeof(NewsanguoCardPool))]
public class LightningStrike : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move),
        new ("PlayMax", 3m),
        new HeavensForceVar(2m)
    ];

    protected override bool ShouldGlowGoldInternal => CanObtainForce;

    private bool CanObtainForce
    {
        get
        {
            var num = CombatManager.Instance.History.CardPlaysFinished.Count(e =>
                    e.HappenedThisTurn(CombatState) && e.CardPlay.Player == Owner);
            return num < DynamicVars["PlayMax"].IntValue;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    public override bool IsHeavensCard => true;

    public LightningStrike() :
        base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        if (CanObtainForce)
        {
            NewsanguoSfx.Play("event:/newsanguo/sfx/lightning_strike");
            await Cmd.CustomScaledWait(0.1f, 0.2f);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_lightning", null, "lightning_orb_evoke.mp3")
                .Execute(choiceContext);
            await HeavensForce.Add(choiceContext, Owner, DynamicVars["HeavensForcePower"].IntValue, this);
        }
        else
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["PlayMax"].UpgradeValueBy(1m);
        DynamicVars["HeavensForcePower"].UpgradeValueBy(1m);
    }
}
