using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NeutralCardPool))]
public class SecretStratagem : NeutralCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得 4 点格挡；满足条件时额外抽 1 张牌（升级不再增加抽牌数）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(4m, ValueProp.Move),
        new CardsVar(1)
    ];

    public override bool GainsBlock => true;

    // 当前手牌数恰好为 3 时金色高亮（打出后剩 2，可触发额外抽牌）
    protected override bool ShouldGlowGoldInternal =>
        PileType.Hand.GetPile(base.Owner).Cards.Count == 3;

    public SecretStratagem() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/plot");

        // 获得 4 点格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay, fast: false);

        // 若打出这张牌后手牌数为 2，则额外抽 1 张牌
        // （打出时这张牌已离开手牌，此时的手牌数即为“打出后”的手牌数）
        if (PileType.Hand.GetPile(base.Owner).Cards.Count == 2)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, base.Owner);
        }
    }

    // 升级：格挡 4 → 7（抽牌效果不再升级）
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
