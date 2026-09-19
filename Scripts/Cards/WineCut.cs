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

    // 卡牌基础数值：造成 7 点伤害（升级 9）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7m, ValueProp.Move)
    ];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

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

        // 造成伤害（享受当前酒力加成）
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 打出此牌后：先将酒力翻倍（再获得等量酒力即翻倍）……
        int currentMight = base.Owner.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0;
        if (currentMight > 0)
        {
            await PowerCmd.Apply<DrunkenMightPower>(
                choiceContext,
                base.Owner.Creature,
                currentMight,
                base.Owner.Creature,
                this,
                silent: false);
        }

        // ……再减半（向下取整），与其他攻击牌打出后的减半规则一致。
        // 本卡不参与 DrunkenMightPower.AfterCardPlayed 的自动减半，减半已在此处手动完成。
        DrunkenMightPower? drunkenMight = base.Owner.Creature.GetPower<DrunkenMightPower>();
        if (drunkenMight is not null)
        {
            await drunkenMight.HalfForCard(choiceContext, this);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 7 提高到 9
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
