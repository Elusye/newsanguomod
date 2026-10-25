using System;
using System.Linq;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Combat;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public class BrightFuturePower : ModPowerTemplate
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png");
    // 用户指定的19种正面效果；每次触发从中不重复抽取。
    private static Func<PlayerChoiceContext, Creature, Task> Grant<T>() where T : PowerModel =>
        async (context, target) => { await PowerCmd.Apply<T>(context, target, 1m, target, null); };
    private static readonly Func<PlayerChoiceContext, Creature, Task>[] Candidates =
    [
        Grant<FreeAttackPower>(), Grant<FreeSkillPower>(), Grant<FreePowerPower>(),
        Grant<DrawCardsNextTurnPower>(), Grant<EnergyNextTurnPower>(),
        Grant<StrengthPower>(), Grant<DexterityPower>(), Grant<VigorPower>(),
        Grant<ThornsPower>(), Grant<PlatingPower>(), Grant<RegenPower>(),
        Grant<GigantificationPower>(), Grant<RitualPower>(), Grant<BufferPower>(),
        Grant<IntangiblePower>(), Grant<DrunkenMightPower>(), Grant<MetallicizePower>(),
        Grant<PlatedArmorPower>(), Grant<FineArmorPower>()
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || !Owner.IsAlive) return;
        var remaining = Candidates.ToList();
        int count = Math.Min(Amount, remaining.Count);
        for (int i = 0; i < count; i++)
        {
            if (!Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) return;
            var grant = player.RunState.Rng.Niche.NextItem(remaining)!;
            remaining.Remove(grant);
            await grant(choiceContext, Owner);
        }
    }
}
