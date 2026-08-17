using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “忽近忽远”赋予的临时力量。回合结束时失去。
/// </summary>
[RegisterPower]
public class near_and_far_strength_power : TemporaryStrengthPower, IModPowerAssetOverrides
{
    public override AbstractModel OriginModel => ModelDb.Card<near_and_far>();

    // 能力图标资源（64x64 普通图标 + 256x256 大图标）
    public PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    public string CustomIconPath => $"res://newsanguo/images/powers/{GetType().Name}.png";
    public string CustomBigIconPath => $"res://newsanguo/images/powers/{GetType().Name}_big.png";
}
