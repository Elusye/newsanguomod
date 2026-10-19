using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using MegaCrit.Sts2.Core.Models.CardPools;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;
namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(TokenCardPool))]
public class AntiMagicField : TerritoryCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/AntiMagicField.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IronCavalryPower>(2m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. base.AdditionalHoverTips, HoverTipFactory.FromPower<IronCavalryPower>(), HeavensForce.HoverTip()
    ];
    public AntiMagicField() : base(-1, CardRarity.Token) { }
    protected override void OnUpgrade() => DynamicVars["IronCavalryPower"].UpgradeValueBy(1);
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        string text = string.Empty;
        if (IsMutable && CombatManager.Instance.IsInProgress && Pile?.IsCombatPile == true)
        {
            var names = Pile.Type == PileType.Hand
                ? Owner.Creature.Powers.Where(AbilitySuppression.IsModPower)
                    .Select(power => power.Title.GetFormattedText()).Distinct().ToArray()
                : System.Array.Empty<string>();
            string separator = new LocString("powers", "NEWSANGUO_POWER_IRON_CAVALRY_POWER.listSeparator").GetFormattedText();
            string list = names.Length == 0
                ? new LocString("powers", "NEWSANGUO_POWER_IRON_CAVALRY_POWER.noSuppressedPowers").GetFormattedText()
                : string.Join(separator, names);
            var line = new LocString("cards", "NEWSANGUO_CARD_ANTI_MAGIC_FIELD.inactivePowers");
            line.Add("Powers", list);
            text = line.GetFormattedText();
        }
        description.Add("InactiveDescription", text);
    }
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner || !Owner.Creature.IsAlive || Pile?.Type != PileType.Hand) return;
        NewsanguoSfx.Play("event:/newsanguo/sfx/anti_magic_field");
        foreach (var enemy in CombatState!.HittableEnemies.Where(enemy => enemy.IsAlive).ToList())
            await PowerCmd.Apply<IronCavalryPower>(context, enemy,
                DynamicVars["IronCavalryPower"].IntValue, Owner.Creature, this);
    }
}
