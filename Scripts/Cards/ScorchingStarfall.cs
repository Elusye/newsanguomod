using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

[RegisterCard(typeof(NewsanguoCardPool))]
public class ScorchingStarfall : NewsanguoCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(2m, ValueProp.Move),
        new ("WineThreshold", 3m),
        new HeavensForceVar(5m),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits").WithMultiplier(static (card, _) =>
        {
            var wine = card.Owner.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0m;
            var per = card is ScorchingStarfall s ? s.DynamicVars["WineThreshold"].IntValue : 3;
            return per > 0 ? Math.Floor(wine / per) : 0m;
        })
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HeavensForce.HoverTip(),
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
        // 出手次数完全由酒力决定，不设上限（这是这张牌的强度设计）
        var hits = threshold > 0 ? wineAmount / threshold : 0;

        if (hits > 0)
        {
            // 但特效整张牌只播一次，不再「每击一组」——原因见 PlayStarfallVfxOnce 的说明
            await PlayStarfallVfxOnce(combatState);

            for (var i = 0; i < hits; i++)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .TargetingAllOpponents(combatState)
                    .Execute(choiceContext);
            }
        }

        await HeavensForce.Add(choiceContext, Owner, -DynamicVars["HeavensForcePower"].IntValue, this);
    }

    /// <summary>
    /// 一次性播放「流星雨」特效：大型魔法飞弹落到敌方场地中心 → 每个敌人脚下点起火焰。
    ///
    /// 旧写法是**每一击**都新建一套（大型飞弹 + 每个敌人一个地面火焰），而且每次还
    /// await 一整个飞弹动画。酒力被翻倍牌（沛国佳酿 / 陶醉 / 不胜酒力 / 百年佳酿…）拉高后，
    /// 「酒力 ÷ 阈值」可以到几十上百，于是一张牌就会创建上百个 VFX 节点，且全部挂在
    /// CombatVfxContainer 里直到战斗结束。实机日志里正是这个形态把 D3D12 的 per-frame
    /// 资源描述符堆塞爆（Cannot bind uniform set … RESOURCES descriptor heap），
    /// 随后驱动判定 GPU 挂死（DXGI_ERROR_DEVICE_HUNG）。
    /// 现在整张牌只播一套，节点数量与酒力无关。
    ///
    /// 原版这两组特效都会自己播放完并在结束时 QueueFreeSafely（NLargeMagicMissileVfx.cs:98-145），
    /// 所以只需要等飞弹「落地」这一小段时间（WaitTime，默认 0.2 秒）来对齐伤害结算，
    /// 不必等整个动画；地面火焰创建后不再等待。
    /// </summary>
    private async Task PlayStarfallVfxOnce(ICombatState combatState)
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

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["WineThreshold"].UpgradeValueBy(-1);
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
    }
}
