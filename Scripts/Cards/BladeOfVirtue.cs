using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.AttackHits;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

[RegisterCard(typeof(NewsanguoCardPool))]
[RegisterCharacterStarterCard(typeof(NewsanguoCharacter), 1)]
public class BladeOfVirtue : NewsanguoCardTemplate, IAttackHitHookListener
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：分两段各造成 3 点伤害；给予目标 1 层虚弱、1 层易伤
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(3, ValueProp.Move),
        new PowerVar<WeakPower>(1m),
        new PowerVar<VulnerablePower>(1m)
    ];

    public BladeOfVirtue() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    // 悬停提示：展示“虚弱”和“易伤”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    // 本次打出正在结算的攻击命令；命中钩子对所有攻击都会触发，用它筛出自己这一条
    private AttackCommand? _currentAttack;

    // 本段正在播放的语音，用于“等它播完再进入下一段”
    private AudioStreamPlayer? _currentVoice;

    // 打出时的效果逻辑：**一次攻击命令打两段**（WithHitCount(2)），
    // 这样依赖“攻击结束”的效果（如 Skittish）会等两段全部打完才结算，
    // 而不是在第一段就触发（原先两段各自 Execute 一次，参考 SSadamune 的 TransparentHole 修复）。
    // 每段自己的语音/负面效果放在逐段钩子里，节奏与原先一致。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        AttackCommand attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_attack_slash");

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

    // 每段命中前：播放该段语音（第一段 blade_of_virtue_1、第二段 blade_of_virtue_2）
    public Task BeforeAttackHit(AttackHitContext context)
    {
        if (context.Attack != _currentAttack)
        {
            return Task.CompletedTask;
        }

        _currentVoice = NewsanguoSfx.Play(context.HitNumber == 1
            ? "event:/newsanguo/sfx/blade_of_virtue_1"
            : "event:/newsanguo/sfx/blade_of_virtue_2");
        return Task.CompletedTask;
    }

    // 每段命中后：结算该段的负面效果，并等本段语音播完再进入下一段（最后一段不等）
    public async Task AfterAttackHit(AttackHitContext context)
    {
        if (context.Attack != _currentAttack)
        {
            return;
        }

        Creature? target = context.SingleTarget;
        if (target is not null)
        {
            if (context.HitNumber == 1)
            {
                await PowerCmd.Apply<WeakPower>(
                    context.ChoiceContext, target, DynamicVars.Weak.IntValue, base.Owner.Creature, this, silent: false);
            }
            else
            {
                await PowerCmd.Apply<VulnerablePower>(
                    context.ChoiceContext, target, DynamicVars.Vulnerable.IntValue, base.Owner.Creature, this, silent: false);
            }
        }

        if (context.HitNumber < context.TotalHitCount)
        {
            await NewsanguoSfx.WaitFinishedAsync(_currentVoice);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 每次伤害从 3 提高到 5
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}
