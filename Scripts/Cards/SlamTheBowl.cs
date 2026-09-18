using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class SlamTheBowl : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 10 点伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(10, ValueProp.Move)
    ];

    public SlamTheBowl() : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/slam_the_bowl");

        // 1. 丢弃所有手牌（快照，避免迭代过程中集合被修改）
        // 当前打出的这张牌通常已不在手牌中，保险起见排除自身

        CardPile handPile = CardPile.Get(PileType.Hand, base.Owner)!;
        List<CardModel> handCards = handPile.Cards
            .Where(c => c != this)
            .ToList();

        if (handCards.Count > 0)
        {
            await CardCmd.Discard(choiceContext, handCards);
        }

        // 2. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 播放伤害音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/slam_the_bowl_damage");

        // 3. 将一张此牌的复制品加入弃牌堆
        // 与原版 Anger 一致：AddGeneratedCardToCombat 本身不会更新弃牌堆 UI 计数，
        // 需配合 PreviewCardPileAdd 生成飞行预览，动画结束时触发 InvokeCardAddFinished 使弃牌堆计数 +1
        CardModel copy = CreateClone();
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Discard, base.Owner), 2.2f);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 10 提高到 12
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}
