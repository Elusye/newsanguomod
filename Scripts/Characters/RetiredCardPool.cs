using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Characters;

/// <summary>Preserves retired card IDs for existing saves.</summary>
[RegisterSharedCardPool]
public class RetiredCardPool : TypeListCardPoolModel
{
    public override string Title => "retired";
    public override string EnergyColorName => "colorless";
    public override string CardFrameMaterialPath => "card_frame_colorless";
    public override Color DeckEntryCardColor => Colors.White;
    public override bool IsColorless => true;
}
