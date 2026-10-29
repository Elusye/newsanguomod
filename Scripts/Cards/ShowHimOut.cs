using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class ShowHimOut : NewsanguoCardTemplate
{
    public ShowHimOut() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/ShowHimOut.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.ForEnergy(this)];
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/show_him_out");
        if (PileType.Hand.GetPile(Owner).Cards.Count == 0) return;
        var card = (await CardSelectCmd.FromHand(context: context, player: Owner,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1, 1),
            filter: null, source: this)).FirstOrDefault();
        if (card is null || card.Pile?.Type != PileType.Hand) return;
        // 在消耗触发其他效果前记录费用；X 费以此刻剩余能量为准。
        int energy = card.EnergyCost.CostsX ? Owner.PlayerCombatState!.Energy :
            Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        if (await CardCmd.Exhaust(context, card) is not null && energy > 0)
            await PlayerCmd.GainEnergy(energy, Owner);
    }
}
