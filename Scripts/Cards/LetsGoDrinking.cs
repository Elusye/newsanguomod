using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class LetsGoDrinking : NewsanguoCardTemplate
{
    public LetsGoDrinking() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/LetsGoDrinking.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DrunkenMightPower>(6m), new DynamicVar("EnemyDrunkenMight", 2m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<DrunkenMightPower>()];

    protected override void OnUpgrade() => DynamicVars["DrunkenMightPower"].UpgradeValueBy(2m);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/lets_go_drinking");
        await PowerCmd.Apply<DrunkenMightPower>(context, Owner.Creature,
            DynamicVars["DrunkenMightPower"].IntValue, Owner.Creature, this);

        if (play.Target is not { IsAlive: true } enemy) return;
        await CreatureCmd.Stun(enemy);
        await PowerCmd.Apply<DrunkenMightPower>(context, enemy,
            DynamicVars["EnemyDrunkenMight"].IntValue, Owner.Creature, this);
    }
}
