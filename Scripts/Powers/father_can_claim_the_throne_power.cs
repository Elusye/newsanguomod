using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “称帝”：在你的回合开始时，失去与层数相同的天意之力，获得与打出次数相同的能量并额外抽等量牌。
/// 每次打出都会叠加：天意之力（Amount）与能量/抽牌数（casts）都增加。
/// 图标显示：右下角为每回合失去的天意之力（Amount），右上角角标为能量/抽牌数。
/// </summary>
[RegisterPower]
public class father_can_claim_the_throne_power : ModPowerTemplate, IPowerExtraIconAmountLabelSpecsProvider, IPowerExtraIconAmountLabelsChangeSource
{
    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 计数器：右下角由原版直接显示每回合失去的天意之力（Amount）
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    private class Data
    {
        // 打出次数：每回合获得的能量数与抽牌数（每次打出 +1）
        public int casts;
    }

    // 初始化实例数据（否则 _internalData 为 null，读取 casts 会抛空引用异常）
    protected override object? InitInternalData()
    {
        return new Data();
    }

    // 角标变化通知：能量/抽牌数变化（不触发 DisplayAmountChanged）时主动刷新图标角标
    public event Action? PowerExtraIconAmountLabelsInvalidated;

    // 描述变量：能量图标（{Energy:energyIcons()}）与抽牌数（{DrawCount}）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new IntVar("DrawCount", 1)
    ];

    // 能力图标角标：右上角显示能量/抽牌数（天意之力由原版右下角层数显示）
    public IReadOnlyList<ExtraIconAmountLabelSpec> GetPowerExtraIconAmountLabelSpecs()
    {
        return
        [
            ExtraIconAmountLabelSpec.Plain(ExtraIconAmountLabelCorner.TopRight, GetInternalData<Data>().casts.ToString()),
        ];
    }

    // 每次打出时能量/抽牌数增加指定值，并同步描述变量与图标角标
    public void AddCast(int count)
    {
        GetInternalData<Data>().casts += count;
        SyncDisplay();
    }

    // 同步能量/抽牌数到描述变量并刷新图标角标
    private void SyncDisplay()
    {
        int casts = GetInternalData<Data>().casts;
        if (DynamicVars.TryGetValue("Energy", out DynamicVar energyVar))
        {
            energyVar.BaseValue = casts;
        }
        if (DynamicVars.TryGetValue("DrawCount", out DynamicVar drawVar))
        {
            drawVar.BaseValue = casts;
        }
        PowerExtraIconAmountLabelsInvalidated?.Invoke();
    }

    // 回合开始时：失去天意之力、获得能量、额外抽牌（均按层数叠加）
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player is null || player.Creature != Owner || Amount <= 0)
        {
            return;
        }

        Flash();

        // 触发“称帝”音效（对应 FMOD 事件 event:/newsanguo/sfx/father_can_claim_the_throne_power）
        SfxCmd.Play("event:/newsanguo/sfx/father_can_claim_the_throne_power");

        // 失去与层数相同的天意之力
        await PowerCmd.Apply<heavens_force>(choiceContext, Owner, -Amount, Owner, null, silent: false);

        // 获得与打出次数相同的能量并抽等量牌
        int casts = GetInternalData<Data>().casts;
        await PlayerCmd.GainEnergy(casts, player);
        await CardPileCmd.Draw(choiceContext, casts, player);
    }
}
