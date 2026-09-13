using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class DeafenMe : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：给予自己的帝王之征层数、造成的伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DragonOmenPower>(3m),
        new DamageVar(15m, ValueProp.Move)
    ];

    // 悬停提示：展示“帝王之征”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DragonOmenPower>()
    ];

    public DeafenMe() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效（静音前的最后一声）
        NewsanguoSfx.Play("event:/newsanguo/sfx/deafen_me");

        // 1. 给予自己若干层帝王之征（自我枷锁）
        int omenAmount = DynamicVars["DragonOmenPower"].IntValue;
        await PowerCmd.Apply<DragonOmenPower>(choiceContext, base.Owner.Creature, omenAmount, base.Owner.Creature, this, silent: false);

        // 2. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 3. 附加“听觉受损”能力，标记本场战斗音量降低状态（战斗结束由能力恢复音量）
        await PowerCmd.Apply<DeafenMePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);

        // 4. 本场战斗中你听到的声音音量降低至 25%：FMOD 主总线与本 mod 的 Godot 播放音效分别降为 1/4。
        // 仅在本机执行（LocalContext.IsMe），否则多人游戏中所有玩家的音频都会被压低
        if (LocalContext.IsMe(base.Owner))
        {
            HearingVolumeController.ReduceToQuarterVolume();
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 15 提高到 20
        DynamicVars.Damage.UpgradeValueBy(5);
    }
}
