using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
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
        IconPath: "res://newsanguo/images/powers/BrightFuturePower.png",
        BigIconPath: "res://newsanguo/images/powers/BrightFuturePowerBig.png");

    internal static bool IsCandidate(CardModel card) => card.Type == CardType.Power &&
        card.Rarity != CardRarity.Ancient &&
        card is not newsanguo.Scripts.Cards.Release and not VoidForm and not Neurosurge and not Royalties;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player.Creature != Owner || !Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) return;
        // 使用全游戏已注册卡牌，不限角色卡池；固定排序保证联机各端的随机候选顺序一致。
        var candidates = ModelDb.AllCards.Where(IsCandidate).DistinctBy(card => card.Id)
            .OrderBy(card => card.Id.ToString(), StringComparer.Ordinal).ToList();
        if (candidates.Count == 0) return;
        // 固定本次触发张数；若随机打出前途灿烂，新层数从下次回合开始生效。
        int count = Amount;
        for (int i = 0; i < count; i++)
        {
            if (!Owner.IsAlive || CombatManager.Instance.IsOverOrEnding || Owner.CombatState is not { } combat)
                break;
            var canonical = candidates[player.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)];
            var card = combat.CreateCard(canonical, player);
            Flash();
            await CardCmd.AutoPlay(context, card, null);
        }
    }
}
