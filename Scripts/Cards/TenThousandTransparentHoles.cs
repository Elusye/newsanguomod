using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class TenThousandTransparentHoles : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 1 点伤害 10 次；击杀时加入 1（升级 2）张复制品
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(1m, ValueProp.Move),
        new CardsVar(1)
    ];

    // 卡牌自带“消耗”关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 悬停提示：说明“击杀”的判定
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Fatal)
    ];

    public TenThousandTransparentHoles() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/ten_thousand_transparent_holes");

        // 造成 1 点伤害 10 次；命中特效套用原版“穿刺”的金色刺击（NStabVfx）；
        // 若此牌击杀了敌人，将 1（2）张此牌的复制品加入手牌（斩杀判定参考原版 KnockoutBlow）
        bool killedEnemy = (await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(10)
            .WithHitVfxNode((Creature t) => NStabVfx.Create(t, facingEnemies: true, VfxColor.Gold))
            .Execute(choiceContext))
            .Results.SelectMany(results => results).Any(result => result.WasTargetKilled);
        if (!killedEnemy)
        {
            return;
        }

        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            CardModel copy = CreateDupe(base.Owner);
            await CardPileCmd.AddGeneratedCardsToCombat([copy], PileType.Hand, base.Owner);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 复制品张数从 1 提高到 2
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
