using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Relics;

/// <summary>
/// 卫星城小沛：每 4 个回合，将一张原版「环绕轨道」（<see cref="Orbit"/>）加入你的手牌。
///
/// 计数方式、角标与点亮时机全部照抄原版「开心小花」（HappyFlower.cs:16-105）：
/// [SavedProperty] 的 TurnsSeen 计数（跨战斗保留）、ShowCounter + DisplayAmount 显示已累计回合数、
/// 差 1 个回合时遗物变 Active、真正触发时先 Flash 再办事、战斗结束只取消点亮不清零。
/// </summary>
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class XiaopeiSatelliteTown : ModRelicTemplate
{
    // 触发周期：每 4 个回合
    private const int TurnsThreshold = 4;

    // 描述里的 {Turns}
    private const string TurnsKey = "Turns";

    private bool _isActivating;

    private int _turnsSeen;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Common;

    // 需要接收战斗钩子，否则 AfterSideTurnStart 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 角标常显（与开心小花一致）
    public override bool ShowCounter => true;

    // 角标：触发瞬间显示周期值（4），其余时候显示已累计的回合数
    public override int DisplayAmount => IsActivating ? DynamicVars[TurnsKey].IntValue : TurnsSeen;

    // 描述中的 {Turns}
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar(TurnsKey, TurnsThreshold)
    ];

    // 悬停时展示「环绕轨道」这张牌的信息
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. HoverTipFactory.FromCardWithCardHoverTips<Orbit>()
    ];

    // 已累计的回合数（存档属性，跨战斗保留，与开心小花一致）
    [SavedProperty]
    public int TurnsSeen
    {
        get => _turnsSeen;
        set
        {
            AssertMutable();
            _turnsSeen = value;
            InvokeDisplayAmountChanged();
        }
    }

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            AssertMutable();
            _isActivating = value;
            InvokeDisplayAmountChanged();
        }
    }

    // 每个回合开始时累计 1；满 4 个回合就加一张「环绕轨道」入手
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }

        TurnsSeen = (TurnsSeen + 1) % DynamicVars[TurnsKey].IntValue;
        // 差 1 个回合时点亮遗物（与开心小花的提示方式一致）
        Status = TurnsSeen == DynamicVars[TurnsKey].IntValue - 1 ? RelicStatus.Active : RelicStatus.Normal;

        if (TurnsSeen != 0)
        {
            return;
        }

        // 视觉动画在后台跑（与开心小花一致：不等它播完就继续给牌）
        _ = TaskHelper.RunSafely(DoActivateVisuals());

        // 原版「环绕轨道」由 CardModel 直接创建，和「魔法禁术目录」造牌走同一条路
        CardModel orbit = combatState.CreateCard<Orbit>(Owner);
        // 本回合免费打出（能量/星费置 0，直到打出或回合结束；与「魔法禁术目录」同款写法）
        orbit.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(orbit, PileType.Hand, Owner);
    }

    // 触发时闪一下，1 秒后角标恢复为累计值（照抄开心小花）
    private async Task DoActivateVisuals()
    {
        IsActivating = true;
        Flash();
        await Cmd.Wait(1f);
        IsActivating = false;
    }

    // 战斗结束时取消点亮状态（计数保留，与开心小花一致）
    public override Task AfterCombatEnd(CombatRoom _)
    {
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
