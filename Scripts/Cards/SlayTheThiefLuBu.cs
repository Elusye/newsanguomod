using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class SlayTheThiefLuBu : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];

    public SlayTheThiefLuBu() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/slay_the_thief_lu_bu");
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // 出牌开始记录已经包含本次；排除它，只统计本玩家此前打出的同一 ID。
        // ID 不随升级变化，也不会把联机队友的出牌算入。
        int previousPlays = CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
            entry.CardPlay != cardPlay && entry.CardPlay.Player == Owner && entry.CardPlay.Card.Id == Id);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(1 + previousPlays)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation location = base.GetResultLocationForCardPlay();
        // 保留复制品离场和外部消耗效果的原版处理。
        if (location.pileType == PileType.Discard)
        {
            location.pileType = PileType.Draw;
            location.position = CardPilePosition.Random;
        }
        return location;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
