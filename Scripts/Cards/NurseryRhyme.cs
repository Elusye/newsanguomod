using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public sealed class NurseryRhyme : NewsanguoCardTemplate
{
    public NurseryRhyme() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/NurseryRhyme.png");
    protected override bool IsScryCard => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ScryAmount", 2m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<NurseryRhymePower>()];

    protected override void OnUpgrade() => DynamicVars["ScryAmount"].UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/nursery_rhyme");
        var power = await PowerCmd.Apply<NurseryRhymePower>(context, Owner.Creature, 1m, Owner.Creature, this);
        if (IsUpgraded && power is not null) power.AddUpgradedTrigger();
    }
}
