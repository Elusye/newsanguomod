using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 多人牌：注册到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WhoRules : NewsanguoCardTemplate
{

    // 仅多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：灾厄层数 7；获得能量 2（升级 +1）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DoomPower>(7m),
        new EnergyVar(2)
    ];

    // 悬停提示：展示“灾厄”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DoomPower>()];

    public WhoRules() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature targetCreature = cardPlay.Target;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/who_rules");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 给予目标玩家 7 层灾厄
        await PowerCmd.Apply<DoomPower>(choiceContext, targetCreature, DynamicVars.Doom.IntValue, base.Owner.Creature, this, silent: false);

        // 你获得 2（3）点能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, base.Owner);
    }

    // 打出后这张牌直接进入目标玩家的抽牌堆（参考原版“球”TheBall 的 GetResultLocationForCardPlay 机制，
    // 不再“创建复制品 + 原卡消耗”）
    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation location = base.GetResultLocationForCardPlay();

        Creature? target = CurrentTarget;
        if (target?.Player is Player targetPlayer && targetPlayer != Owner)
        {
            location.player = targetPlayer;
            location.pileType = PileType.Draw;
            location.position = CardPilePosition.Random;
        }
        return location;
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 获得能量从 2 提高到 3
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
