using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class ProxyStrike : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 14 点伤害（升级 20）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(14m, ValueProp.Move)
    ];

    // 标签：视为“打击”，使依赖打击标签的遗物/卡牌可与之交互
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    public ProxyStrike() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        NewsanguoSfx.Play("event:/newsanguo/sfx/proxy_strike");

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 选择一张手牌中的攻击牌，将该牌的两张复制品加入手牌（参考原版“双重施放”DualWield）
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext,
            player: base.Owner,
            filter: card => card.Type == CardType.Attack,
            source: this)).ToList();
        CardModel? sourceCard = selected.FirstOrDefault();
        if (sourceCard is null)
        {
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            CardModel copy = sourceCard.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, base.Owner, CardPilePosition.Random);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 14 提高到 20
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}
