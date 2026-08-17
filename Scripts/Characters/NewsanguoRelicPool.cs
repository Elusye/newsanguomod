using System;
using System.Collections.Generic;
using newsanguo.Scripts.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Characters;

[RegisterSharedRelicPool]
public class NewsanguoRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "newsanguo";

    [Obsolete("基类要求保留。")]
    protected override IEnumerable<Type> RelicTypes => [typeof(fine_brew_of_pei)];
}
