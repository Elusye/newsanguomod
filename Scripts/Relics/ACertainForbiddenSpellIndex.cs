using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Relics;

// 魔法禁术目录：在第 2/3/5 个回合开始时，将一张随机禁术牌加入你的手牌，
// 那张禁术牌在该回合免费打出，且打出时不消耗天意之力。
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class ACertainForbiddenSpellIndex : ModRelicTemplate
{
    private const string ForbiddenSpellKeywordId = "NEWSANGUO_KEYWORD_FORBIDDEN_SPELL";

    // 触发回合：第 2/3/5 个回合开始时各触发一次
    private static readonly int[] TriggerTurns = [2, 3, 5];

    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    // 战斗中的回合数内显示角标
    public override bool ShowCounter => DisplayAmount > -1;

    // 角标：当前回合数（每回合 +1，第一回合显示 1）；三次触发用完后隐藏
    public override int DisplayAmount
    {
        get
        {
            if (!CombatManager.Instance.IsInProgress || IsCanonical ||
                Owner.PlayerCombatState?.TurnNumber is not { } turnNumber)
            {
                return -1;
            }

            return turnNumber > TriggerTurns[^1] ? -1 : turnNumber;
        }
    }

    // 悬停时展示“天意之力”“天意侵蚀”能力与“禁术牌”关键词说明
    // （天意之力的说明文本中会出现“天意侵蚀”，两者须成对展示）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>(),
        ModKeywordRegistry.CreateHoverTip(ForbiddenSpellKeywordId)
    ];

    // 每个回合开始时刷新角标；第 2/3/5 个回合额外生成一张随机禁术牌加入手牌
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner.PlayerCombatState?.TurnNumber is not { } turnNumber)
        {
            return;
        }

        // 回合数已变化，刷新角标
        InvokeDisplayAmountChanged();

        if (!TriggerTurns.Contains(turnNumber))
        {
            return;
        }

        Flash();

        // 候选池：四张禁术牌（人体炼成术 / 将领延寿术 / 心灵控制术 / 亡灵复活术）
        List<CardModel> pool = [
            ModelDb.Card<HumanTransmutationSpell>(),
            ModelDb.Card<LongevitySpell>(),
            ModelDb.Card<MindControlSpell>(),
            ModelDb.Card<ReanimationSpell>()
        ];

        CardModel card = CardFactory
            .GetDistinctForCombat(Owner, pool, 1, Owner.RunState.Rng.CombatCardGeneration)
            .First();

        // 本回合免费打出（能量消耗为 0）
        card.SetToFreeThisTurn();

        // 本回合打出时不消耗天意之力
        if (card is NewsanguoCardTemplate forbiddenSpell)
        {
            forbiddenSpell.SetToFreeHeavensForceThisTurn();
        }

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
    }

    // 抵消被标记禁术牌的天意之力消耗（只对遗物持有者自己打出的那张牌生效）
    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount,
        Creature? target, CardModel? cardSource)
    {
        if (giver != Owner.Creature || amount >= 0m || power is not HeavensForcePower)
        {
            return 0m;
        }

        if (cardSource is NewsanguoCardTemplate { IsFreeHeavensForceThisTurn: true })
        {
            return -amount;
        }

        return 0m;
    }
}
