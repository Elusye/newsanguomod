using newsanguo.Scripts.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace newsanguo.Scripts.Powers;

[STS2RitsuLib.Interop.AutoRegistration.RegisterPower]
public sealed class OldMythicFormPower : STS2RitsuLib.Scaffolding.Content.ModPowerTemplate
{
	private const string _upgradedKey = "Upgraded";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Upgraded", 0m)];

	protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.Static(StaticHoverTip.Tarot)];

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player != base.Owner.Player)
		{
			return;
		}
		Flash();
        // 全部原版塔罗牌均参与；七张改版牌使用0.113旧实体，其他保持原版。
		int upgradedCount = base.DynamicVars["Upgraded"].IntValue;
		for (int i = 0; i < base.Amount; i++)
		{
			CardModel? cardModel = player.RunState.Rng.CombatCardSelection.NextItem(TarotCards.All.Select(card => card switch
                {
                    TheHierophant => ModelDb.Card<OldTheHierophant>(),
                    TheEmperor => ModelDb.Card<OldTheEmperor>(),
                    TheTower => ModelDb.Card<OldTheTower>(),
                    Temperance => ModelDb.Card<OldTemperance>(),
                    TheSun => ModelDb.Card<OldTheSun>(),
                    TheHermit => ModelDb.Card<OldTheHermit>(),
                    Death => ModelDb.Card<OldDeath>(),
                    _ => card
                }).Where((CardModel c) => c.IsAllowedInRun(player.RunState)));
			if (cardModel != null)
			{
				CardModel card = combatState.CreateCard(cardModel, player);
				if (i < upgradedCount)
				{
					CardCmd.Upgrade(card);
				}
				await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
			}
		}
	}

	public void IncrementUpgraded()
	{
		AssertMutable();
		base.DynamicVars["Upgraded"].BaseValue++;
	}
}
