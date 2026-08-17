using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天意操控”：所有在战斗中临时增加的牌将被升级。
/// 通过 AfterCardGeneratedForCombat 钩子，在任意战斗生成牌（技能、药水、能力等）被加入战斗后立即升级。
/// </summary>
[RegisterPower]
public class fate_control_power : ModPowerTemplate
{
    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：单一（效果不随层数变化）
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 需要战斗生成牌的钩子
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 战斗生成牌时触发（参考原版 ArsenalPower 的校验方式：只影响本玩家生成的牌）
    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (Owner is null || creator is null || creator.Creature != Owner)
        {
            return Task.CompletedTask;
        }

        // 已升级的牌无需再次升级
        if (card.IsUpgraded)
        {
            return Task.CompletedTask;
        }

        CardCmd.Upgrade(card);

        // 牌已加入手牌，刷新其显示以同步升级后的名称/描述
        NCard.FindOnTable(card)?.UpdateVisuals(card.Pile?.Type ?? PileType.Hand, CardPreviewMode.None);
        return Task.CompletedTask;
    }
}
