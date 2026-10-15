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

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(ShuHanCardPool))]
public class DeafenMe : NewsanguoCardTemplate
{
    public override bool IsTigerDragonCard => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DragonOmenPower>(15m)];

    // 悬停提示：展示“帝王之征”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DragonOmenPower>()
    ];

    public DeafenMe() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效（静音前的最后一声）
        NewsanguoSfx.Play("event:/newsanguo/sfx/deafen_me");

        // 给予选中的敌人15/20层帝王之征。
        await PowerCmd.Apply<DragonOmenPower>(choiceContext, cardPlay.Target,
            DynamicVars["DragonOmenPower"].IntValue, Owner.Creature, this);

        // 3. 附加“听觉受损”能力，标记本场战斗音量降低状态（战斗结束由能力恢复音量）
        await PowerCmd.Apply<DeafenMePower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this);

        // 4. 本场战斗中你听到的声音音量降低25%（保留75%）：FMOD 主总线与本 mod 的 Godot 播放音效分别保留75%。
        // 仅在本机执行（LocalContext.IsMe），否则多人游戏中所有玩家的音频都会被压低
        if (LocalContext.IsMe(base.Owner))
        {
            HearingVolumeController.ReduceVolumeByQuarter();
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 帝王之征从15提高到20
        DynamicVars["DragonOmenPower"].UpgradeValueBy(5);
    }
}
