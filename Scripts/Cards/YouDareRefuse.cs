using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 「竟然不许！」（类名仍是 YouDareRefuse，卡面名字见本地化）——**选择卡**，不是可打出的牌：
/// 与 <see cref="FollowHeavensWill"/> 一起构成回合结束时那次二选一。选它 = 拒绝本次正向转化，
/// **不执行任何操作**：不消耗天意之力、不给“双倍伤害”、不进入额外回合；
/// 点数保留在 ≥10，下个回合结束时仍会再问。选它时会播一句「竟然不许！」语音（纯表现）。
/// 真正判定在 <see cref="Powers.HeavensForcePower"/> 里。
/// </summary>
[RegisterCard(typeof(StatusCardPool))]
public class YouDareRefuse : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 状态牌：不能升级、不参与随机生成
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    // 属于“天意”体系（与 HeavensDecay 等状态/诅咒牌一致）
    public override bool IsHeavensCard => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip()
    ];

    public YouDareRefuse() : base(-1, CardType.Status, CardRarity.Status, TargetType.None)
    {
    }
}
