using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WineCut : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 7 点伤害（升级 9）；获得 2 点酒力（升级 3）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<DrunkenMight>("drunken_might", 2)
    ];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMight>()];

    public WineCut() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/wine_cut");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 造成伤害（享受当前酒力加成）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        // 酒力效果：打出攻击牌后酒力减半（失去酒力）
        DrunkenMight? drunkenMight = base.Owner.Creature.GetPower<DrunkenMight>();
        if (drunkenMight is not null)
        {
            await drunkenMight.HalfForCard(choiceContext, this);
        }

        // 获得酒力（卡牌效果）
        await PowerCmd.Apply<DrunkenMight>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["drunken_might"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 7 提高到 9，酒力从 2 提高到 3
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["drunken_might"].UpgradeValueBy(1);
    }
}
