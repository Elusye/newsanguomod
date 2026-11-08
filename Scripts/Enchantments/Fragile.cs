using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Enchantments;

[RegisterEnchantment]
public sealed class Fragile : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new(IconPath: "res://newsanguo/images/enchantments/Fragile.png");

    protected override void OnEnchant() => Card.AddKeyword(CardKeyword.Innate);

    public override int EnchantPlayCount(int originalPlayCount) => originalPlayCount + 1;
}
