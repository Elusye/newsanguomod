using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Relics;

// 百年佳酿：初始遗物“沛国佳酿”的先古版本（由原版 Orobas 事件的“触摸俄瑞波斯”替换获得）
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class CenturyBrew : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>(7m),
        new IntVar("TurnEndWine", 2)
    ];

    // 悬停提示：展示“酒力”能力说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    public override bool ShouldReceiveCombatHooks => true;

    public override Task BeforeCombatStart()
    {
        if (Owner?.Creature is null)
            return Task.CompletedTask;

        return PowerCmd.Apply<DrunkenMightPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["DrunkenMightPower"].IntValue, Owner.Creature, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Owner?.Creature is not { IsAlive: true } creature ||
            !participants.Contains(creature))
            return;

        Flash();
        await PowerCmd.Apply<DrunkenMightPower>(choiceContext, creature,
            DynamicVars["TurnEndWine"].IntValue, creature, null);
    }
}
