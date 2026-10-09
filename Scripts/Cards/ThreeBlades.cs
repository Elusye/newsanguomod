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
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class ThreeBlades : NewsanguoCardTemplate
{
    // 击杀时获得的能量
    private const int EnergyGain = 3;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 9（升级 12）点伤害 3 次
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(9, ValueProp.Move),
        new RepeatVar(3)
    ];

    // 鼠标悬停：本牌与酒力互动，补一条酒力说明（含"打出攻击牌后减半"的规则）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];

    public ThreeBlades() : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        NewsanguoSfx.Play("event:/newsanguo/sfx/three_blades");

        // 造成 9（12）点伤害 3 次；斩杀判定参考原版 KnockoutBlow
        bool killedEnemy = (await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext))
            .Results.SelectMany(results => results).Any(result => result.WasTargetKilled);

        // 本牌不参与 DrunkenMightPower.AfterCardPlayed 的自动减半（那里的减半按"打出瞬间层数"结算），
        // 所以两条分支都在此处手动完成。
        if (killedEnemy)
        {
            // 击杀：返还此牌消耗的酒力（先减半消耗、再把消耗掉的部分加回来 → 净不损失酒力）
            DrunkenMightPower? killMight = base.Owner.Creature.GetPower<DrunkenMightPower>();
            if (killMight is not null)
            {
                await killMight.ConsumeThenRefund(choiceContext, this);
            }

            // 并额外获得 3 点能量
            await PlayerCmd.GainEnergy(EnergyGain, base.Owner);
            return;
        }

        // 未击杀：与其他攻击牌一致，按打出瞬间的层数减半（向下取整）
        DrunkenMightPower? drunkenMight = base.Owner.Creature.GetPower<DrunkenMightPower>();
        if (drunkenMight is not null)
        {
            await drunkenMight.HalfForCard(choiceContext, this);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 9 提高到 12
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
