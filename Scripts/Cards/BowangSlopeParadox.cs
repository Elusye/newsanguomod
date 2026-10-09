using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到蜀汉卡池（只在本池注册，曹魏拿不到）
[RegisterCard(typeof(ShuHanCardPool))]
public class BowangSlopeParadox : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：抽 3 张牌（升级后 4 张）
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    // 悬停提示：这两张效果都施加在抽到的牌上，这里把关键词含义显示出来
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        HoverTipFactory.FromKeyword(CardKeyword.Unplayable)
    ];

    public BowangSlopeParadox() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/bowang_slope_paradox");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 抽牌
        List<CardModel> drawn = (await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, base.Owner)).ToList();
        if (drawn.Count == 0)
        {
            return;
        }

        // 抽到的牌本回合「保留」：引擎自带的单回合保留，回合结束时自动清除
        foreach (CardModel card in drawn)
        {
            CardCmd.ApplySingleTurnRetain(card);
        }

        // 抽到的牌本回合「不能被打出」：把牌登记给「悖论」能力，由能力的全局关键词钩子加 Unplayable；
        // 能力会在玩家回合结束时自动移除，锁定随之失效。
        ParadoxPower? power = await PowerCmd.Apply<ParadoxPower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this,
            silent: false);
        power?.LockCards(drawn);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 抽牌数从 3 提高到 4 (3+1)
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
