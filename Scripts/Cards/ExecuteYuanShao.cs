using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public sealed class ExecuteYuanShao : NewsanguoCardTemplate
{
    public override bool IsHeavensCard => true;
    public override bool LosesHeavensForce => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: "res://newsanguo/images/cards/ExecuteYuanShao.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ExecuteThreshold", 25m),
        new HeavensForceVar(5m),
        new DamageVar(12m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HeavensForce.HoverTip()];

    public ExecuteYuanShao() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override bool ShouldGlowGoldInternal =>
        Pile?.Type == PileType.Hand && CombatState?.HittableEnemies.Any(CanExecute) == true;

    private bool CanExecute(Creature target) =>
        target.IsAlive && target.CurrentHp * 100m <= target.MaxHp * DynamicVars["ExecuteThreshold"].BaseValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var target = cardPlay.Target;
        if (CanExecute(target))
        {
            await PlayExecutionIntro();

            NewsanguoSfx.Play("event:/newsanguo/sfx/execute_yuan_shao_3");
            if (target.IsAlive)
            {
                NDebugAudioManager.Instance?.Play("heavy_attack.mp3");
                var vfx = target.GetVfxContainer();
                vfx?.AddChildSafely(NBigSlashVfx.Create(target));
                vfx?.AddChildSafely(NBigSlashImpactVfx.Create(target));
                await HeavensForce.Add(choiceContext, Owner, -DynamicVars["HeavensForcePower"].IntValue, this);
                if (target.IsAlive)
                    await CreatureCmd.SetCurrentHp(target, 0m);
            }
        }
        else
        {
            NewsanguoSfx.Play("event:/newsanguo/sfx/execute_yuan_shao_1");
        }

        if (target.IsAlive)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this, cardPlay)
                .Targeting(target).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
        }
    }

    private async Task PlayExecutionIntro()
    {
        const string secondVoice = "res://newsanguo/audios/execute_yuan_shao_2.mp3";

        using var playback = NewsanguoSfx.CapturePlayback();
        var voice = NewsanguoSfx.Play(secondVoice);
        if (NCombatRoom.Instance is { } room)
        {
            double voiceDuration = voice?.Stream?.GetLength() ?? 0;
            double leadIn = Math.Max(0, voiceDuration * Engine.TimeScale - NGrandFinaleVfx.totalAnticipationDuration) + 0.5;
            TaskHelper.RunSafely(StartFinaleAfterDelay(room, (float)leadIn));
        }

        await playback.WaitAsync();
    }

    private async Task StartFinaleAfterDelay(NCombatRoom room, float delay)
    {
        await Cmd.Wait(delay);
        if (GodotObject.IsInstanceValid(room) && room.IsInsideTree() && NCombatRoom.Instance == room &&
            NGrandFinaleVfx.Create(Owner.Creature) is { } finale)
        {
            room.CombatVfxContainer.AddChildSafely(finale);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1m);
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
