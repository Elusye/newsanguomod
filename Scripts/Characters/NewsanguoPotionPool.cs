using System;
using System.Collections.Generic;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Characters;

[RegisterSharedPotionPool]
public class NewsanguoPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "newsanguo";

    [Obsolete("基类要求保留。")]
    protected override IEnumerable<Type> PotionTypes => Array.Empty<Type>();
}
