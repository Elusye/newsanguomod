using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

namespace newsanguo.Scripts.Combat;

/// <summary>
/// 自动打牌辅助：让玩家从右到左自动打出手牌，最多打满 <see cref="MaxCardsToPlay"/> 张。
/// 参考原版遗物“低语耳环”（WhisperingEarring）的自动打牌逻辑，仅将取牌方向改为从右到左。
/// </summary>
public static class AutoPlayRightToLeft
{
    public const int MaxCardsToPlay = 13;

    /// <summary>
    /// 玩家从右到左自动打出手牌（上限 <see cref="MaxCardsToPlay"/> 张）。
    /// </summary>
    /// <param name="choiceContext">选择上下文。</param>
    /// <param name="owner">被自动打牌的玩家。</param>
    public static async Task PlayHandRightToLeftAsync(PlayerChoiceContext choiceContext, Player owner)
    {
        if (owner?.Creature is null) return;

        ICombatState? combatState = owner.Creature.CombatState;
        if (combatState is null) return;
        var playerCombatState = owner.PlayerCombatState;
        if (playerCombatState is null) return;

        // 推送选择器，避免自动打牌过程弹出卡牌选择界面（与原版低语耳环一致）
        using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
        {
            int cardsPlayed = 0;
            int startTurn = playerCombatState.TurnNumber;
            for (; cardsPlayed < MaxCardsToPlay; cardsPlayed++)
            {
                if (CombatManager.Instance.IsOverOrEnding)
                {
                    break;
                }
                if (CombatManager.Instance.IsPlayerReadyToEndTurn(owner))
                {
                    break;
                }
                if (playerCombatState.TurnNumber != startTurn)
                {
                    break;
                }

                CardPile pile = PileType.Hand.GetPile(owner);
                // 从右到左：取手牌中最右侧一张能打出的牌
                CardModel? card = pile.Cards.LastOrDefault(c => c.CanPlay());
                if (card == null)
                {
                    break;
                }

                Creature? target = GetTarget(card, combatState, owner);
                await card.SpendResources();
                await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
            }
        }
    }

    /// <summary>
    /// 获取自动打出卡牌的目标。
    /// 敌人：最右侧的敌人优先。友方：随机。玩家：自身。
    /// </summary>
    private static Creature? GetTarget(CardModel card, ICombatState combatState, Player owner)
    {
        Rng combatTargets = owner.RunState.Rng.CombatTargets;
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.LastOrDefault(),
            TargetType.AnyAlly => combatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive && c.IsPlayer && c != owner.Creature)),
            TargetType.AnyPlayer => owner.Creature,
            _ => null,
        };
    }
}
