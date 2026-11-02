using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NeutralCardPool))]
public class StandardCamp : NeutralCardTemplate
{
    public StandardCamp() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/StandardCamp.png");
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate, CardKeyword.Exhaust, .. base.CanonicalKeywords];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FineArmorPower>(6m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<FineArmorPower>()];
    protected override void OnUpgrade() => DynamicVars["FineArmorPower"].UpgradeValueBy(2m);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/standard_camp");
        await PowerCmd.Apply<FineArmorPower>(context, Owner.Creature,
            DynamicVars["FineArmorPower"].BaseValue, Owner.Creature, this);
    }
    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler != Owner || !IsInCombat || CombatManager.Instance.IsOverOrEnding ||
            Pile is null || Pile.Type is PileType.Hand or PileType.Play or PileType.None or PileType.Deck)
            return;
        // 移动原牌，消耗堆中的本体也能回手，不创建复制品。
        await CardPileCmd.Add(this, PileType.Hand);
    }
}
