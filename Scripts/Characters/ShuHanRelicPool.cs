using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Relics;
using STS2RitsuLib.Interop.AutoRegistration;

namespace newsanguo.Scripts.Characters;

/// <summary>沿用共用遗物，但排除曹魏专属的魔法禁术目录。</summary>
[RegisterSharedRelicPool]
public class ShuHanRelicPool : NewsanguoRelicPool
{
    [Obsolete("基类要求保留。")]
    protected override IEnumerable<Type> RelicTypes => ModelDb.RelicPool<NewsanguoRelicPool>().AllRelics
        .Where(relic => relic is not ACertainForbiddenSpellIndex)
        .Select(relic => relic.GetType()).Distinct();
}
