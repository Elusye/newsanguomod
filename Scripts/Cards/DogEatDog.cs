using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Combat.CardTargeting;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class DogEatDog : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];
    public DogEatDog() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/dog_eat_dog");
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        Creature? first = cardPlay.Target;
        if (first?.Monster is null || !first.IsAlive || CombatManager.Instance.IsOverOrEnding)
            return;
        var others = CombatState!.HittableEnemies.Where(c => c != first && c.IsAlive && c.Monster is not null).ToList();
        if (others.Count == 0)
            return;
        Creature? second = Owner.RunState.Rng.CombatTargets.NextItem(others);
        if (second is null)
            return;
        // 快照双方招式中的原始伤害和段数，实际攻击时正常计算力量、虚弱、易伤等修正。
        var firstAttacks = SnapshotAttacks(first);
        if (firstAttacks.Length == 0)
            return;
        var secondAttacks = SnapshotAttacks(second);
        await AttackWithIntent(choiceContext, first, second, firstAttacks);
        await AttackWithIntent(choiceContext, second, first, secondAttacks);
    }

    private static (decimal Damage, int Hits)[] SnapshotAttacks(Creature creature) =>
        creature.Monster!.NextMove.Intents.OfType<AttackIntent>()
            .Select(a => (Math.Max(0m, a.DamageCalc?.Invoke() ?? 0m), Math.Max(1, a.Repeats))).ToArray();

    private static async Task AttackWithIntent(PlayerChoiceContext context, Creature attacker, Creature target,
        (decimal Damage, int Hits)[] attacks)
    {
        foreach (var attack in attacks)
        {
            if (!attacker.IsAlive || !target.IsAlive || CombatManager.Instance.IsOverOrEnding)
                break;
            // FromMonster 默认攻击玩家，使用框架接口将目标限定为另一只怪物。
            await DamageCmd.Attack(attack.Damage).FromMonster(attacker.Monster!)
                .TargetingFiltered([target]).WithHitCount(attack.Hits)
                .WithHitFx("vfx/vfx_attack_slash").Execute(context);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}
