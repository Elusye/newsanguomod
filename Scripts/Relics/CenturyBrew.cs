using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
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

    // 描述中的 {drunken_might}：每个回合开始时获得的酒力层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>(4m)
    ];

    // 悬停提示：展示“酒力”能力说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    public override bool ShouldReceiveCombatHooks => true;

    // 每个回合开始时：获得 4 点酒力（多人下只在 Owner 自己的回合触发）
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature is null)
        {
            return;
        }

        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext: choiceContext,
            target: Owner.Creature,
            amount: DynamicVars["DrunkenMightPower"].IntValue,
            applier: Owner.Creature,
            cardSource: null,
            silent: false);
    }
}
