using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Relics;

/// <summary>
/// 天子经济学：每场战斗开始时，将 1 张「天子」（<see cref="SonOfHeaven"/>）加入你的手牌。
/// </summary>
[RegisterRelic(typeof(NewsanguoRelicPool))]
public class SonOfHeavenEconomics : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/relics/{GetType().Name}.png",
        IconOutlinePath: $"res://newsanguo/images/relics/{GetType().Name}Outline.png",
        BigIconPath: $"res://newsanguo/images/relics/{GetType().Name}Big.png"
    );

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    // 需要接收战斗钩子，否则 BeforeHandDraw 不会被调用
    public override bool ShouldReceiveCombatHooks => true;

    // 悬停时展示「天子」这张牌的信息
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        .. HoverTipFactory.FromCardWithCardHoverTips<SonOfHeaven>()
    ];

    /// <summary>
    /// 每场战斗开始时（首回合抽牌前）将 1 张「天子」加入手牌。
    ///
    /// 时机沿用原版「工具箱」（Toolbox.cs:22-34）：BeforeHandDraw + 首回合判定。
    /// 放在抽牌前，加入的这张牌会和起手 5 张一起出现在手里；首回合判定保证每场战斗只加一次。
    /// 联机下本钩子会在每台机器上按同一顺序执行（钩子派发见 Hook.cs:588-601），
    /// 两端各自把这张牌按相同结果加入该玩家的手牌，不会产生分歧。
    /// </summary>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner || player.PlayerCombatState is not { TurnNumber: 1 })
        {
            return;
        }

        Flash();
        CardModel sonOfHeaven = combatState.CreateCard<SonOfHeaven>(player);
        await CardPileCmd.AddGeneratedCardToCombat(sonOfHeaven, PileType.Hand, player);
    }
}
