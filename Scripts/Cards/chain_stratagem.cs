using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 多人牌：注册到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class chain_stratagem : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：罕见
    private const CardRarity rarity = CardRarity.Uncommon;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 仅多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 6 点伤害（升级 8）；每次打出后本场战斗伤害 +3（升级 +4）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move),
        new IntVar("increment", 3)
    ];

    // 战斗内共享的打出次数计数器：
    // 连表计会在盟友间不断传递同一张牌，本场战斗中可能被多次打出，
    // 用战斗实例作键统计“第几次打出”以区分奇偶音效，战斗结束随实例自动回收。
    private static readonly ConditionalWeakTable<ICombatState, PlayCounter> _playCounters = new();

    private sealed class PlayCounter
    {
        public int Value;
    }

    public chain_stratagem() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        Creature? targetCreature = cardPlay.Target;
        if (owner is null || targetCreature is null)
        {
            return;
        }

        // 播放出牌音效：第奇数次与第偶数次打出使用不同的事件
        // 对应 FMOD 事件 event:/newsanguo/sfx/chain_stratagem_odd / _even
        PlayCounter counter = _playCounters.GetOrCreateValue(CombatState);
        counter.Value++;
        bool isOddPlay = (counter.Value & 1) == 1;
        SfxCmd.Play(isOddPlay
            ? "event:/newsanguo/sfx/chain_stratagem1"
            : "event:/newsanguo/sfx/chain_stratagem2");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 1. 造成当前伤害（尚未包含本次加成，与“夷陵之火”一致）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(targetCreature)
            .Execute(choiceContext);

        // 2. 再增加这张牌在本场战斗中的伤害（每次打出按固定值递增，只增加自己，与“夷陵之火”的全体递增不同）
        DynamicVars.Damage.BaseValue += DynamicVars["increment"].IntValue;
    }

    // 打出后这张牌直接进入一名随机盟友的手牌（参考原版“球”TheBall 的 GetResultLocationForCardPlay 机制，
    // 不再“创建复制品 + 原卡消耗”；牌在同一实例上持续累积伤害并不断传递）
    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation location = base.GetResultLocationForCardPlay();
        if (CombatState is null)
        {
            return location;
        }

        List<Creature> allies = CombatState.GetTeammatesOf(Owner.Creature)
            .Where(c => c != Owner.Creature && c.IsAlive && c.IsPlayer)
            .ToList();
        if (allies.Count > 0)
        {
            // 用战斗同步 RNG 随机选盟友，保证多人两端结果一致（同 TheBall）
            location.player = Owner.RunState.Rng.CombatTargets.NextItem(allies).Player!; // 已按 IsPlayer 过滤，Player 必非空
            location.pileType = PileType.Hand;
            location.position = CardPilePosition.Random;
        }
        return location;
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 6 提高到 8；单次递增从 3 提高到 4
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["increment"].UpgradeValueBy(1);
    }
}
