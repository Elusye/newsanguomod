using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class HelplessWorry : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(16m, ValueProp.Move)];
    public override bool GainsBlock => true;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<Doubt>()];

    public HelplessWorry() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/helpless_worry");
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay, fast: false);
        // 三个目标牌堆等概率选择，抽牌堆中的插入位置也随机。
        PileType destination = Owner.RunState.Rng.CombatCardGeneration.NextInt(3) switch
        {
            0 => PileType.Draw,
            1 => PileType.Discard,
            _ => PileType.Hand
        };
        var generated = CombatState!.CreateCard<Doubt>(Owner);
        var result = await CardPileCmd.AddGeneratedCardToCombat(
            generated, destination, Owner, position: CardPilePosition.Random);
        if (destination != PileType.Hand)
            CardCmd.PreviewCardPileAdd(result);
        await Cmd.Wait(0.5f);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
