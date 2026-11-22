using MegaCrit.Sts2.Core.HoverTips;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 2026-10-09：按要求从「曹魏」（新三国）卡池移除，改为只注册在蜀汉卡池。
[RegisterCard(typeof(ShuHanCardPool))]
public class FallOnOwnSword : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的能量、抽牌数、本回合内每打出一张牌获得的污染层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(2),
        new CardsVar(3),
        new PowerVar<TaintedPower>(3m)
    ];

    // 卡牌自带“消耗”关键词（合并 base 以保留模板附加的模组关键词）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.ForEnergy(this), HoverTipFactory.FromPower<TaintedPower>()];

    public FallOnOwnSword() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/fall_on_own_sword");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, base.Owner);

        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].IntValue, base.Owner);

        // 本回合后续每打出一张牌获得污染，本牌自身不触发新施加的能力。
        await PowerCmd.Apply<BeatOfDeathPower>(choiceContext, base.Owner.Creature, DynamicVars["TaintedPower"].IntValue, base.Owner.Creature, this);
    }

    // 升级：抽牌 3 → 5
    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(2);
    }
}
