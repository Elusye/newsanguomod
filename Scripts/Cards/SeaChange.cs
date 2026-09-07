using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Helpers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class SeaChange : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public SeaChange() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/sea_change");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        CardPile hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count == 0)
        {
            return;
        }

        // 逐个随机变化所有手牌（先快照列表，避免变换过程中集合变化）
        Rng rng = base.Owner.RunState.Rng.CombatCardSelection;
        List<CardModel> originals = hand.Cards.ToList();

        foreach (CardModel original in originals)
        {
            CardPileAddResult result = await CardCmd.TransformToRandom(original, rng);
            // 升级后：为变化出来的牌施加随机附魔
            if (IsUpgraded && result.cardAdded != null)
            {
                EnchantHelper.ApplyRandomEnchant(result.cardAdded, base.Owner);
            }
        }
    }
}
