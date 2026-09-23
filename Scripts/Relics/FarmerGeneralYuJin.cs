using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Relics;

/// <summary>
/// 种地将军于禁（商店遗物）：拾起时从牌组中选择 3 张牌，为这些牌附魔「播种」（原版附魔 Sown）。
///
/// 写法照抄原版「三刃回旋镖」（TriBoomerang.cs:15-35）：同一套选牌接口
/// <c>CardSelectCmd.FromDeckForEnchantment</c>（会打开带附魔预览的牌组选择界面，且选择过程走
/// PlayerChoiceSynchronizer，多人下两端一致）+ <c>CardCmd.Enchant</c> + 附魔特效。
/// 牌数用 CardsVar(3)，与卡面文案的 {Cards} 同源；
/// 牌组里可附魔的牌不足 3 张时，该接口会自动全选（CardSelectCmd.cs:576-579），不会报错。
/// </summary>
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class FarmerGeneralYuJin : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Shop;

    // 效果只在拾起时生效
    public override bool HasUponPickupEffect => true;

    // 描述里的 {Cards}
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new CardsVar(3)
    ];

    // 悬停时展示「播种」附魔的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. HoverTipFactory.FromEnchantment<Sown>()
    ];

    // 拾起时：从牌组中选 3 张牌附魔「播种」
    public override async Task AfterObtained()
    {
        foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, DynamicVars.Cards.IntValue),
            player: Owner,
            enchantment: ModelDb.Enchantment<Sown>(),
            amount: 1))
        {
            CardCmd.Enchant<Sown>(card, 1m);

            // 附魔特效（与原版三刃回旋镖一致）
            NCardEnchantVfx? enchantVfx = NCardEnchantVfx.Create(card);
            if (enchantVfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(enchantVfx);
            }
        }
    }
}
