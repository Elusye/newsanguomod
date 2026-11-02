using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class OneForSorrowTwoForJoy : NewsanguoCardTemplate
{
    public OneForSorrowTwoForJoy() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/OneForSorrowTwoForJoy.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<OneForSorrowTwoForJoyPower>(1m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.FromPower<DrunkenMightPower>()];
    protected override void OnUpgrade() => DynamicVars["OneForSorrowTwoForJoyPower"].UpgradeValueBy(1m);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/one_for_sorrow_two_for_joy");
        await PowerCmd.Apply<OneForSorrowTwoForJoyPower>(context, Owner.Creature,
            DynamicVars["OneForSorrowTwoForJoyPower"].BaseValue, Owner.Creature, this);
    }
}
