using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Enchantments;

[RegisterEnchantment]
public sealed class FineTuning : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new(IconPath: "res://newsanguo/images/enchantments/FineTuning.png");

    [SavedProperty]
    public int BlockAdjustment { get; set; }

    public override bool CanEnchant(CardModel card) =>
        card.Type == CardType.Skill && card.GainsBlock && base.CanEnchant(card);
    public override bool ShowAmount => true;
    public override int DisplayAmount => BlockAdjustment;

    public override decimal EnchantBlockAdditive(decimal originalBlock) =>
        Math.Max(-originalBlock, BlockAdjustment);

    private static decimal GetPreviewBlock(CardModel card)
    {
        if (card.CombatState is not { } combat) return 0m;
        if (card.DynamicVars.TryGetValue("CalculatedBlock", out var calculated) && calculated is CalculatedBlockVar calculatedBlock)
            return Hook.ModifyBlock(combat, card.Owner.Creature, calculatedBlock.Calculate(null),
                calculatedBlock.Props, card, null, out _);
        if (card.DynamicVars.TryGetValue("Block", out var fixedBlock) && fixedBlock is BlockVar block)
            return Hook.ModifyBlock(combat, card.Owner.Creature, block.BaseValue, block.Props, card, null, out _);
        return 0m;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (card != Card) return Task.CompletedTask;

        bool decrease = GetPreviewBlock(card) > 15m || card.Owner.RunState.Rng.Niche.NextInt(2) == 0;
        BlockAdjustment += decrease ? -1 : 1;
        if (card.DeckVersion?.Enchantment is FineTuning original)
            original.BlockAdjustment = BlockAdjustment;
        card.DynamicVars.RecalculateForUpgradeOrEnchant();
        return Task.CompletedTask;
    }
}
