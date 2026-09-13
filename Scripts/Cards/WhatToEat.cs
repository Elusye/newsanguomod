using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 多人牌：注册到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WhatToEat : NewsanguoCardTemplate
{

    // 仅多人模式可用
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 鼠标悬停时显示“干饭”卡牌标注（升级时显示升级版干饭）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<ChowDown>(IsUpgraded)
    ];

    public WhatToEat() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 往所有玩家的手牌中加入一张“干饭”（升级后加入升级版的干饭）
        // 在 await 前计算玩家列表，避免空引用流分析失效
        var combatState = CombatState!;

        List<Creature> players = combatState.GetTeammatesOf(base.Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer)
            .ToList();

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/what_to_eat");

        foreach (Creature player in players)
        {
            CardModel chowDown = combatState.CreateCard<ChowDown>(player.Player);
            if (IsUpgraded)
            {
                chowDown.UpgradeInternal();
                chowDown.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(chowDown, PileType.Hand, base.Owner, CardPilePosition.Random);
        }
    }
}
