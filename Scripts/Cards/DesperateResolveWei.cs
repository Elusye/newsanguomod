using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class DesperateResolveWei : NewsanguoCardTemplate
{
    public DesperateResolveWei() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/CaoArtOfWar.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(5)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [RecruitKeyword.Recruit(DynamicVars.Summon), HoverTipFactory.FromPower<StrengthPower>()];

    protected override void OnUpgrade() => DynamicVars.Summon.UpgradeValueBy(1m);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/cao_art_of_war");
        await PowerCmd.Apply<CaoArtOfWarPower>(context, Owner.Creature,
            DynamicVars.Summon.IntValue, Owner.Creature, this);
    }
}
