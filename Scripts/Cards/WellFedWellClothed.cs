using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

/// <summary>不缺吃穿：获得酒力与多层护甲，升级后均由 4 提高到 6。</summary>
[RegisterCard(typeof(NeutralCardPool))]
public class WellFedWellClothed : NeutralCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>(4m), new PowerVar<PlatedArmorPower>(4m)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HoverTipFactory.FromPower<PlatedArmorPower>()
    ];

    public WellFedWellClothed() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/well_fed_well_clothed");
        await PowerCmd.Apply<DrunkenMightPower>(choiceContext, Owner.Creature,
            DynamicVars["DrunkenMightPower"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<PlatedArmorPower>(choiceContext, Owner.Creature,
            DynamicVars["PlatedArmorPower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DrunkenMightPower"].UpgradeValueBy(2);
        DynamicVars["PlatedArmorPower"].UpgradeValueBy(2);
    }
}
