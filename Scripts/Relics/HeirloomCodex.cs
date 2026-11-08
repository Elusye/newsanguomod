using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Relics;

[RegisterRelic(typeof(NewsanguoRelicPool))]
public sealed class HeirloomCodex : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/relics/HeirloomCodex.png",
        IconOutlinePath: "res://newsanguo/images/relics/HeirloomCodexOutline.png",
        BigIconPath: "res://newsanguo/images/relics/HeirloomCodexBig.png");

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        HoverTipFactory.FromEnchantment<Fragile>();

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        CardModel? lastCard = CombatManager.Instance.History.CardPlaysStarted
            .LastOrDefault(entry => entry.CardPlay.Player == Owner)?.CardPlay.Card;
        var fragile = ModelDb.Enchantment<Fragile>();
        if (lastCard is null || !fragile.CanEnchant(lastCard.CanonicalInstance))
        {
            return;
        }

        // 保留升级等级，战斗内的临时变化和旧附魔不带入牌组。
        CardModel copy = Owner.RunState.CreateCard(lastCard.CanonicalInstance, Owner);
        for (int i = 0; i < lastCard.CurrentUpgradeLevel; i++)
        {
            copy.UpgradeInternal();
            copy.FinalizeUpgradeInternal();
        }

        Flash();
        CardCmd.Enchant(fragile.ToMutable(), copy, 1);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(copy, PileType.Deck));
    }
}
