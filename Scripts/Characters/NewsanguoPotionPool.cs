using System;
using System.Collections.Generic;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Characters;

[RegisterSharedPotionPool]
public class NewsanguoPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "newsanguo";

    // 共用药水继续使用通用能量图标，不依赖角色卡池的能量标识。
    public override string? BigEnergyIconPath => "res://newsanguo/images/ui/energy_newsanguo.png";
    public override string? TextEnergyIconPath => "res://newsanguo/images/ui/energy_newsanguo_small.png";

    [Obsolete("基类要求保留。")]
    protected override IEnumerable<Type> PotionTypes => Array.Empty<Type>();
}
