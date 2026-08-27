using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Relics;

[RegisterRelic(typeof(NewsanguoRelicPool))]
[RegisterCharacterStarterRelic(typeof(NewsanguoCharacter))]
public class fine_brew_of_pei : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}_outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}_big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Starter;

    // 悬停提示：展示“酒力”能力说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<drunken_might>()];

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature is null) return;

        await PowerCmd.Apply<drunken_might>(
            choiceContext: null!,
            target: Owner.Creature,
            amount: 4,
            applier: Owner.Creature,
            cardSource: null,
            silent: false);
    }
}
