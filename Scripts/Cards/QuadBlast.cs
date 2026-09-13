using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

[RegisterCard(typeof(NewsanguoCardPool))]
public class QuadBlast : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(4m, ValueProp.Move),
        new RepeatVar(4)
    ];

    public QuadBlast() :
        base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;
        
        for (var i = 1; i <= 4; i++)
        {
            var player = NewsanguoSfx.Play($"event:/newsanguo/sfx/quad_blast_{i}");

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(CombatState)
                .WithAttackerAnim("Cast", 0.2f)
                .BeforeDamage(async delegate
                {
                    var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
                    var beamVfx = NHyperbeamVfx.Create(Owner.Creature, enemies.Last());
                    if (beamVfx != null)
                    {
                        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(beamVfx);
                        await Cmd.Wait(0.2f);
                    }
                    foreach (var impactVfx in enemies.Select(enemy => NHyperbeamImpactVfx.Create(Owner.Creature, enemy)).OfType<NHyperbeamImpactVfx>())
                    {
                        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(impactVfx);
                    }
                })
                .Execute(choiceContext);

            if (i < 4)
            {
                await NewsanguoSfx.WaitFinishedAsync(player);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
