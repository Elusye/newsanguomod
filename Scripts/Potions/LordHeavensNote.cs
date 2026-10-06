using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using newsanguo.Scripts.Api;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Potions;

/// <summary>
/// 天意爷的小纸条（Lord Heaven's Note，原「天意药水」）：获得 3 点天意之力
/// （战斗内使用，可对任一玩家使用）。
/// 数值同时写在 <see cref="Amount"/> 与本地化文案里（药水不升级，故不需要动态变量）。
///
/// 注册：<c>[RegisterPotion(typeof(NewsanguoPotionPool))]</c> 会由 RitsuLib 注入到共享药水池
/// （池本身已标 <c>[RegisterSharedPotionPool]</c>，会追加进 <c>ModelDb.AllPotionPools</c>），
/// 两个角色（曹魏/蜀汉）都以该池作为 PotionPool —— 所以只写这一条特性即可在商店/奖励里出现。
/// 模型 id 形如 <c>NEWSANGUO_POTION_LORD_HEAVENS_NOTE</c>，本地化键即以此为前缀。
/// </summary>
[RegisterPotion(typeof(NewsanguoPotionPool))]
public sealed class LordHeavensNote : ModPotionTemplate
{
    /// <summary>获得的天意之力点数（改数值时同步改 zhs/eng 的 potions.json 文案）。</summary>
    private const int Amount = 3;

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    /// <summary>
    /// 药水图标（背包/奖励/悬浮提示用）。原版药水图取自 atlas，模组药水改走这里的路径覆盖。
    /// 文件（<c>newsanguo/images/potions/LordHeavensNote.png</c>）还没放进来时，RitsuLib 会
    /// 因为资源不存在而跳过这次覆盖（<c>ContentAssetOverridePatchHelper.TryUseStringOverride</c>
    /// 的 <c>requireExistingResource</c> 默认 true），回退到原版「缺失药水」占位图，不会报错崩溃；
    /// 图片放进来（并 <c>--import</c>）后无需改代码即可直接显示。
    /// 轮廓图（OutlinePath）留空：原版在缺省时会自行回退，不影响显示。
    /// </summary>
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: "res://newsanguo/images/potions/LordHeavensNote.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("d4af37"));

        // 天意之力必须走 HeavensForce 的封装（不要对隐藏载体直接 PowerCmd.Apply）
        Player player = target?.Player ?? Owner;
        await NewsanguoPublicApi.AddHeavensForce(choiceContext, player, Amount, source: null);
    }
}
