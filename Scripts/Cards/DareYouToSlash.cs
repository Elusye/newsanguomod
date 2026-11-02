using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NeutralCardPool))]
public class DareYouToSlash : NeutralCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(1m), new PowerVar<DrunkenMightPower>(8m)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<DrunkenMightPower>()
    ];
    public DareYouToSlash() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/dare_you_to_slash");
        var vulnerable = await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature,
            DynamicVars["VulnerablePower"].IntValue, Owner.Creature, this);
        // 与无情剑法一致：当轮敌方回合结束即递减，不跳过首次持续时间结算。
        if (vulnerable is not null)
            vulnerable.SkipNextDurationTick = false;
        await PowerCmd.Apply<DrunkenMightPower>(choiceContext, Owner.Creature,
            DynamicVars["DrunkenMightPower"].IntValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["DrunkenMightPower"].UpgradeValueBy(3);
}
