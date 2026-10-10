using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

/// <summary>募集青州兵，在下回合开始前为己方青州兵的死亡提供天意之力。</summary>
[RegisterCard(typeof(NewsanguoCardPool))]
public class WorthAllTheirLives : NewsanguoCardTemplate
{
    public override bool IsHeavensCard => true;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new SummonVar(5)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RecruitKeyword.Recruit(DynamicVars.Summon), HeavensForce.HoverTip()
    ];

    public WorthAllTheirLives() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/worth_all_their_lives");
        // 先挂载能力，使募集期间发生的死亡也能获得奖励。
        await PowerCmd.Apply<WorthAllTheirLivesPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        WorthAllTheirLivesPower power = Owner.Creature.GetPower<WorthAllTheirLivesPower>()!;
        power.RefreshExpiry();
        for (int i = 0; i < DynamicVars.Summon.IntValue; i++)
        {
            var soldier = await CaoArtOfWarPower.SummonIndependentSoldier(choiceContext, Owner);
            if (soldier is null)
            {
                break;
            }
            power.TrackSoldier(soldier);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Summon.UpgradeValueBy(2);
}
