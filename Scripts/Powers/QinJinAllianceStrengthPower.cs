using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “秦晋之好”施加的临时力量降低。回合结束时恢复。
/// </summary>
[RegisterPower]
public class QinJinAllianceStrengthPower : TemporaryStrengthPower, IModPowerAssetOverrides
{
    public override AbstractModel OriginModel => ModelDb.Card<QinJinAlliance>();

    protected override bool IsPositive => false;

    // 能力图标资源（64x64 普通图标 + 256x256 大图标）
    public PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/NearAndFarStrengthPower.png",
        BigIconPath: $"res://newsanguo/images/powers/NearAndFarStrengthPowerBig.png"
    );

    public string CustomIconPath => $"res://newsanguo/images/powers/NearAndFarStrengthPower.png";
    public string CustomBigIconPath => $"res://newsanguo/images/powers/NearAndFarStrengthPowerBig.png";
}
