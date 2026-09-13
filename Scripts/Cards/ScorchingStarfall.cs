using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

[RegisterCard(typeof(NewsanguoCardPool))]
public class ScorchingStarfall : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(2m, ValueProp.Move),
        new ("WineThreshold", 3m),
        new PowerVar<HeavensForcePower>(5m),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits").WithMultiplier(static (card, _) =>
        {
            var wine = card.Owner.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0m;
            var per = card is ScorchingStarfall s ? s.DynamicVars["WineThreshold"].IntValue : 3;
            return per > 0 ? Math.Floor(wine / per) : 0m;
        })
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    public override bool IsHeavensCard => true;

    public ScorchingStarfall() :
        base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/scorching_starfall");

        var combatState = CombatState!;

        var wineAmount = Owner.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0;
        var threshold = DynamicVars["WineThreshold"].IntValue;
        var hits = threshold > 0 ? wineAmount / threshold : 0;

        if (hits > 0)
        {
            for (var i = 0; i < hits; i++)
            {
                if (CombatState != null)
                {
                    var sideCenterFloor = VfxCmd.GetSideCenterFloor(CombatSide.Enemy, CombatState);
                    if (sideCenterFloor.HasValue)
                    {
                        var val = NLargeMagicMissileVfx.Create(sideCenterFloor.Value, new Color("917cf6"));
                        if (val != null)
                        {
                            var instance = NCombatRoom.Instance;
                            instance?.CombatVfxContainer.AddChildSafely(val);
                            await Cmd.Wait(val.WaitTime);
                        }
                    }

                    foreach (var hittableEnemy in CombatState.HittableEnemies)
                    {
                        var instance2 = NCombatRoom.Instance;
                        instance2?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(hittableEnemy));
                    }
                }

                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .TargetingAllOpponents(combatState)
                    .Execute(choiceContext);
            }
        }

        await PowerCmd.Apply<HeavensForcePower>(choiceContext, Owner.Creature, -DynamicVars["HeavensForcePower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["WineThreshold"].UpgradeValueBy(-1);
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
    }
}
