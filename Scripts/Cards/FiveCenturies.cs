using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class FiveCenturies : NewsanguoCardTemplate
{
    public FiveCenturies() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/FiveCenturies.png");
    protected override bool IsScryCard => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ScryAmount", 5m)];
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
    protected override void AfterDowngraded() => RemoveKeyword(CardKeyword.Retain);

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/five_centuries");
        await ScryCmd.Scry(context, Owner, DynamicVars["ScryAmount"].IntValue);
        while (!CombatManager.Instance.IsOverOrEnding && Owner.Creature.IsAlive)
        {
            if (CombatManager.Instance.IsOverOrEnding || !Owner.Creature.IsAlive) return;
            // 与原版从抽牌堆自动打牌一致：空堆时先尝试洗入弃牌。
            await CardPileCmd.ShuffleIfNecessary(context, Owner);
            var top = PileType.Draw.GetPile(Owner).Cards.FirstOrDefault();
            if (top is null) return;
            bool isAttack = top.Type == CardType.Attack;
            int historyCount = CombatManager.Instance.History.CardPlaysStarted.Count();
            await CardCmd.AutoPlay(context, top, null);
            // 若被无法打出等规则阻止，不再重复；仅实际打出的攻击牌延续流程。
            if (!isAttack || !CombatManager.Instance.History.CardPlaysStarted.Skip(historyCount)
                .Any(entry => entry.CardPlay.Card == top)) return;
        }
    }
}
