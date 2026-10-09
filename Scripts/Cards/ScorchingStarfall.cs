using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(NewsanguoCardPool))]
public class ScorchingStarfall : NewsanguoCardTemplate
{
    /// <summary>
    /// 播放特效的段数上限：段数 ≤ 这个数时**逐段播放**特效（与旧版一致），超过就整张牌完全不播
    /// （正好 10 段仍然播）。段数一多就说明酒力被堆得很高，这时特效既没有观赏价值
    /// （伤害数字已经糊满屏），又会让 VFX 节点数随酒力无限增长，推高单帧的 GPU 资源开销
    /// （见 <see cref="PlayStarfallVfxPerHit" /> 的说明）。想调档位就改这一个数。
    /// </summary>
    private const int MaxHitsForVfx = 10;

    /// <summary>
    /// 出手次数的硬上限：无论酒力被叠到多高，「酒力 ÷ 阈值」都不会超过这个数。
    /// 只是给这张牌的强度加一道天花板（正常情况下永远摸不到），同时保证循环次数与卡面
    /// 显示的次数（<c>CalculatedHits</c>）用的是同一个上限，二者不会对不上。
    /// </summary>
    private const int MaxHits = 114514;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌数值：每 WineThreshold 点酒力打一次 Damage 点伤害（历史：曾额外"失去 5 点天意之力"，
    // 已按需求移除，因此不再有 HeavensForceVar，升级也不再降低消耗）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(5m, ValueProp.Move),
        new ("WineThreshold", 2m),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits").WithMultiplier(static (card, _) =>
        {
            var wine = card.Owner.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0m;
            var per = card is ScorchingStarfall s ? s.DynamicVars["WineThreshold"].IntValue : 3;
            return per > 0 ? Math.Min(Math.Floor(wine / per), MaxHits) : 0m;
        })
    ];

    // 悬停提示：本牌只与酒力互动（已不再获取/失去天意之力，故不再挂天意之力的说明）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 特效资源在跑图时预载（照搬原版 ForgottenRitual / 本 mod「旧遗忘仪式」的写法），
    // 少一堆 "Asset not cached" 的即时报错
    protected override IEnumerable<string> ExtraRunAssetPaths => [
        .. NGroundFireVfx.AssetPaths,
        NLargeMagicMissileVfx.scenePath
    ];

    public override bool IsHeavensCard => true;

    public ScorchingStarfall() :
        base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/scorching_starfall");

        var combatState = CombatState!;

        var wineAmount = Owner.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0;
        var threshold = DynamicVars["WineThreshold"].IntValue;
        // 出手次数由酒力决定，但一道硬上限托底（见 MaxHits：正常玩法永远摸不到）
        var hits = threshold > 0 ? Math.Min(wineAmount / threshold, MaxHits) : 0;

        if (hits > 0)
        {
            // 段数 ≤ MaxHitsForVfx 时：特效**逐段播放**，与旧版完全一致（每段一套飞弹 + 地面火焰）；
            // 段数超过上限时整张牌一个特效都不播（正好 10 段仍然播）。
            // 见 PlayStarfallVfxPerHit 说明：逐段播的节点数最多是 MaxHitsForVfx × (1 + 敌人数)，
            // 不会像以前那样随酒力无限增长。
            var playVfxPerHit = hits <= MaxHitsForVfx;

            for (var i = 0; i < hits; i++)
            {
                if (playVfxPerHit)
                {
                    await PlayStarfallVfxPerHit(combatState);
                }

                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .TargetingAllOpponents(combatState)
                    .Execute(choiceContext);
            }
        }
    }

    /// <summary>
    /// 播放一段「流星雨」特效（旧版写法，**每段调用一次**）：
    /// 大型魔法飞弹落到敌方场地中心 → 每个敌人脚下点起火焰。
    ///
    /// 原版这两组特效都会自己播放完并在结束时 QueueFreeSafely（NLargeMagicMissileVfx.cs:98-145），
    /// 所以这里只需要等飞弹「落地」这一小段时间（WaitTime，默认 0.2 秒）来对齐伤害结算，
    /// 不必等整个动画；地面火焰创建后不再等待。
    ///
    /// 这是最初那套逐段特效的写法，唯一区别是**调用条件**：只有段数 ≤ MaxHitsForVfx 时才会被调用。
    /// 之所以要加这个条件：酒力被翻倍牌（沛国佳酿 / 陶醉 / 不胜酒力 / 百年佳酿…）拉高后，
    /// 「酒力 ÷ 阈值」可以到几十上百，逐段播就会创建上百套 VFX 节点并全部挂在
    /// CombatVfxContainer 里直到战斗结束——实机日志里正是这个形态把 D3D12 的 per-frame
    /// 资源描述符堆塞爆（Cannot bind uniform set … RESOURCES descriptor heap），
    /// 随后驱动判定 GPU 挂死（DXGI_ERROR_DEVICE_HUNG）。
    /// 段数多的时候不播特效，节点数从此有上限。
    /// </summary>
    private async Task PlayStarfallVfxPerHit(ICombatState combatState)
    {
        if (VfxCmd.GetSideCenterFloor(CombatSide.Enemy, combatState) is { } sideCenterFloor)
        {
            var missile = NLargeMagicMissileVfx.Create(sideCenterFloor, new Color("917cf6"));
            if (missile != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(missile);
                await Cmd.Wait(missile.WaitTime);
            }
        }

        foreach (var enemy in combatState.HittableEnemies)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(enemy));
        }
    }

    // 升级：每次伤害 5 → 7，并获得"保留"（Retain，回合结束时留在手牌）
    // （历史：此前升级是"触发阈值 3 → 2"与"失去的天意之力 5 → 4"，均已按需求替换/移除）
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        AddKeyword(CardKeyword.Retain);
    }
}
