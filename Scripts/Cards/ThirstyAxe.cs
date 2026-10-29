using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NeutralCardPool))]
public class ThirstyAxe : NeutralCardTemplate
{
    public ThirstyAxe() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/ThirstyAxe.png");
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14m, ValueProp.Move), new DynamicVar("WineMultiplier", 3m)];
    public decimal WineMultiplier => DynamicVars["WineMultiplier"].BaseValue;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<DrunkenMightPower>()];
    protected override void OnUpgrade() => DynamicVars["WineMultiplier"].UpgradeValueBy(2m);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        NewsanguoSfx.Play("event:/newsanguo/sfx/thirsty_axe");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, play)
            .Targeting(play.Target).Execute(context);
    }
}
