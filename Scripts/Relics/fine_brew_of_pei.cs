using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

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

    // 描述中的 {drunken_might}：每场战斗开始时获得的酒力层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>("drunken_might", 4)
    ];

    // 悬停提示：展示“酒力”能力说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature is null) return;

        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext: null!,
            target: Owner.Creature,
            amount: DynamicVars["drunken_might"].IntValue,
            applier: Owner.Creature,
            cardSource: null,
            silent: false);
    }
}
