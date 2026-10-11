using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class TimeAcidRain : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    public override bool IsHeavensCard => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(5m), new DamageVar(10m, ValueProp.Move),
        new DynamicVar("IronCavalry", 2m)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<ArtifactPower>()
    ];

    public TimeAcidRain() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
        DynamicVars["IronCavalry"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/time_acid_rain");
        await HeavensForce.Add(choiceContext, Owner, -DynamicVars["HeavensForcePower"].IntValue, this);
        // 先破防与施加铁骑，使本次攻击也不受敌方正面能力保护。
        foreach (var enemy in CombatState!.GetOpponentsOf(Owner.Creature).Where(c => c.IsAlive).ToList())
        {
            await CreatureCmd.LoseBlock(choiceContext, enemy, enemy.Block, Owner.Creature);
            // 先移除人工制品，避免其消耗层数抵消铁骑减益。
            if (enemy.GetPower<ArtifactPower>() is { } artifact)
            {
                await PowerCmd.Remove(artifact);
            }
            await PowerCmd.Apply<IronCavalryPower>(choiceContext, enemy,
                DynamicVars["IronCavalry"].IntValue, Owner.Creature, this);
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState!)
            .Execute(choiceContext);
    }
}
