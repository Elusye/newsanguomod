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

    // 卡牌基础数值：造成 1 点伤害 10 次；击杀时加入 1 张复制品
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(1m, ValueProp.Move),
        new CardsVar(1)
    ];

    // 虚无 + 消耗（升级后仅移除虚无，保留消耗）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

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
            CardModel copy = CreateClone();
            await CardPileCmd.AddGeneratedCardsToCombat([copy], PileType.Hand, base.Owner);
        }
    }

    // 升级：去除虚无，保留消耗
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }

    // 降级：恢复“虚无”
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Ethereal);
    }
}
