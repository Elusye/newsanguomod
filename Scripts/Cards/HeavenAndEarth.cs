using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using MegaCrit.Sts2.Core.Models.CardPools;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(TokenCardPool))]
public class HeavenAndEarth : TerritoryCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/HeavenAndEarth.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FlightPower>(1m)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. base.AdditionalHoverTips, HoverTipFactory.FromPower<FlightPower>()
    ];
    public HeavenAndEarth() : base(-1, CardRarity.Token) { }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        // 回合开始抽牌后判定；仅持有者的手牌本体触发，抽牌堆及弃牌堆不触发。
        if (player != Owner || !Owner.Creature.IsAlive || Pile?.Type != PileType.Hand)
            return;
        NewsanguoSfx.Play("event:/newsanguo/sfx/heaven_and_earth");
        await PowerCmd.Apply<FlightPower>(choiceContext, Owner.Creature,
            DynamicVars["FlightPower"].IntValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["FlightPower"].UpgradeValueBy(1);
}
