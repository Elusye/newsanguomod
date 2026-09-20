using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Relics;

// 空城计：你的回合结束时若手牌为空，则你本回合（直到敌方回合结束）受到的攻击伤害减半。
// 减伤通过施加“钻石头冠（旧）”能力实现，该能力会在敌方回合结束时自行移除。
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class EmptyCityStratagem : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 允许接收战斗钩子，否则 BeforeSideTurnEnd 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 你的回合结束时：若手牌为空，则获得“钻石头冠（旧）”
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }

        if (!PileType.Hand.GetPile(Owner).IsEmpty)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<OldDiamondDiademPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null);
    }
}
