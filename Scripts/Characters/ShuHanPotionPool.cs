using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Potions;
using STS2RitsuLib.Interop.AutoRegistration;

namespace newsanguo.Scripts.Characters;

/// <summary>蜀汉沿用其他模组药水，排除曹魏专属的天意爷的小纸条。</summary>
[RegisterSharedPotionPool]
public class ShuHanPotionPool : NewsanguoPotionPool
{
    [Obsolete("基类要求保留。")]
    protected override IEnumerable<Type> PotionTypes => ModelDb.PotionPool<NewsanguoPotionPool>().AllPotions
        .Where(potion => potion is not LordHeavensNote)
        .Select(potion => potion.GetType()).Distinct();
}
