using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using newsanguo.Scripts.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

[RegisterPower]
public sealed class NurseryRhymePower : ModPowerTemplate
{
    private int _upgradedTriggers;
    private int _nextVoice = 1;

    internal void AddUpgradedTrigger() => _upgradedTriggers++;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/NurseryRhymePower.png",
        BigIconPath: "res://newsanguo/images/powers/NurseryRhymePowerBig.png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || !Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) return;

        int count = Amount;
        int upgraded = Math.Min(_upgradedTriggers, count);
        Flash();
        int voice = _nextVoice;
        _nextVoice = voice == 4 ? 1 : voice + 1;
        string variant = voice == 3 && Random.Shared.Next(2) == 0 ? "_special" : "";
        NewsanguoSfx.Play($"event:/newsanguo/sfx/nursery_rhyme_{voice}{variant}");
        for (int i = 0; i < count; i++)
        {
            if (!Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) break;
            await ScryCmd.Scry(context, player, i < upgraded ? 3 : 2);
            if (!Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) break;
            await CardPileCmd.Draw(context, 1m, player);
        }
    }
}
