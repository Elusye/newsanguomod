using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using newsanguo.Scripts.Api;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Potions;

/// <summary>
/// 杏花村（Apricot Blossom Brew，原「酒力药水」）：获得 4 点酒力
/// （战斗内使用，可对任一玩家使用，与「力量药水」同形）。
/// 数值同时写在 <see cref="Amount"/> 与本地化文案里（药水不升级，故不需要动态变量）。
///
/// 注册：<c>[RegisterPotion(typeof(NewsanguoPotionPool))]</c> 会由 RitsuLib 注入到共享药水池
/// （池本身已标 <c>[RegisterSharedPotionPool]</c>，会追加进 <c>ModelDb.AllPotionPools</c>），
/// 两个角色（曹魏/蜀汉）都以该池作为 PotionPool —— 所以只写这一条特性即可在商店/奖励里出现。
/// 模型 id 形如 <c>NEWSANGUO_POTION_APRICOT_BLOSSOM_BREW</c>，本地化键即以此为前缀。
/// </summary>
[RegisterPotion(typeof(NewsanguoPotionPool))]
public sealed class ApricotBlossomBrew : ModPotionTemplate
{
    /// <summary>获得的酒力层数（改数值时同步改 zhs/eng 的 potions.json 文案）。</summary>
    private const decimal Amount = 4m;

    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    /// <summary>
    /// 药水图标（背包/奖励/悬浮提示用）。原版药水图取自 atlas，模组药水改走这里的路径覆盖。
    /// 轮廓图（OutlinePath）留空：原版在缺省时会自行回退，不影响显示。
    /// </summary>
    public override PotionAssetProfile AssetProfile => new(
        ImagePath: "res://newsanguo/images/potions/ApricotBlossomBrew.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        NCombatRoom.Instance?.PlaySplashVfx(target, new Color("c0392b"));

        // 走「酒力」的公开入口（与卡牌施加同一条路径，含层数封顶等保护）
        await NewsanguoPublicApi.ApplyDrunkenMight(
            choiceContext,
            target!,
            Amount,
            Owner?.Creature,
            cardSource: null);
    }
}
