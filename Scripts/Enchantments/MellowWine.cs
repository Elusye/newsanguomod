using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Enchantments;

[RegisterEnchantment]
public sealed class MellowWine : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new(IconPath: "res://newsanguo/images/enchantments/MellowWine.png");

    public override bool CanEnchant(CardModel card) => card.Type == CardType.Attack && base.CanEnchant(card);
}
