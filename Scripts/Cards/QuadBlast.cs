using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.AttackHits;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

[RegisterCard(typeof(NewsanguoCardPool))]
public class QuadBlast : NewsanguoCardTemplate, IAttackHitHookListener
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(4m, ValueProp.Move),
        new RepeatVar(4)
    ];

    public QuadBlast() :
        base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    // 本次打出正在结算的攻击命令；命中钩子对所有攻击都会触发，用它筛出自己这一条
    private AttackCommand? _currentAttack;

    // 本段正在播放的语音，用于“等它播完再进入下一段”
    private AudioStreamPlayer? _currentVoice;

    // 打出时的效果逻辑：**一次攻击命令打四段**（WithHitCount(4)），
    // 这样依赖“攻击结束”的效果（如 Skittish）会等四段全部打完才结算，
    // 而不是在第一段就触发（原先四段各自 Execute 一次，参考 SSadamune 的 TransparentHole 修复）。
    // 每段自己的施法动作/语音/光束特效放在逐段钩子里，节奏与原先一致。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitCount(4);

        _currentAttack = attack;
        try
        {
            await attack.Execute(choiceContext);
        }
        finally
        {
            _currentAttack = null;
        }
    }

    // 每段命中前：光束特效 → 该段语音（quad_blast_1..4）→ 施法动作
    // （原先这几项挂在每个单段命令的 WithAttackerAnim / BeforeDamage 上，等于每段各来一次；
    //   改成单命令后必须放进逐段钩子，否则只剩第一段有表现）
    public async Task BeforeAttackHit(AttackHitContext context)
    {
        if (context.Attack != _currentAttack)
        {
            return;
        }

        // 先出光束与命中特效
        List<Creature> enemies = context.CombatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count > 0)
        {
            NHyperbeamVfx? beamVfx = NHyperbeamVfx.Create(base.Owner.Creature, enemies.Last());
            if (beamVfx != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(beamVfx);
                await Cmd.Wait(0.2f);
            }

            foreach (NHyperbeamImpactVfx impactVfx in enemies.Select(enemy => NHyperbeamImpactVfx.Create(base.Owner.Creature, enemy)).OfType<NHyperbeamImpactVfx>())
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(impactVfx);
            }
        }

        // 再起该段语音与施法动作
        _currentVoice = NewsanguoSfx.Play($"event:/newsanguo/sfx/quad_blast_{context.HitNumber}");

        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", 0.2f);
    }

    // 每段命中后：等本段语音播完再进入下一段（第四段语音播完不需要等待）
    public async Task AfterAttackHit(AttackHitContext context)
    {
        if (context.Attack != _currentAttack)
        {
            return;
        }

        if (context.HitNumber < context.TotalHitCount)
        {
            await NewsanguoSfx.WaitFinishedAsync(_currentVoice);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}
