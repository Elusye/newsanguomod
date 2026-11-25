using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Scaffolding.Content;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace newsanguo.Scripts.Cards;

[STS2RitsuLib.Interop.AutoRegistration.RegisterCard(typeof(TokenCardPool))]
public sealed class OldTheSun : NewsanguoCardTemplate
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<ColorlessCardPool>();
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override CardAssetProfile AssetProfile => new(PortraitPath: ModelDb.Card<TheSun>().PortraitPath);

	public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips
	{
		get
		{
			List<IHoverTip> list = new List<IHoverTip>();
			list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<Soot>());
			list.Add(base.EnergyHoverTip);
			return list;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => [
		new CardsVar(2),
		new EnergyVar(2)
	];

	public OldTheSun()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
		await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
		await CardPileCmd.AddToCombatAndPreview<Soot>(base.Owner.Creature, PileType.Discard, 1, base.Owner);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Energy.UpgradeValueBy(1m);
		base.DynamicVars.Cards.UpgradeValueBy(1m);
	}
}
