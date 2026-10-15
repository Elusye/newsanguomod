using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

/// <summary>狂妄之人：获得2层易伤，每回合额外抽1张牌；升级后额外抽2张。</summary>
[RegisterCard(typeof(ShuHanCardPool))]
public class Megalovania : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：自身易伤固定2层、每回合额外抽牌1（升级后2）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(2m),
        new CardsVar(1)
    ];

    // 0 费、稀有、能力牌、目标为自己
    public Megalovania() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 悬停提示：展示易伤说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>()

    ];

    // 打出时的效果逻辑：先给自己叠易伤（代价），再挂上「狂妄之人」能力（每回合额外抽牌）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/megalovania");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 代价：给自己叠易伤（升级前后均为2层）
        VulnerablePower? vulnerable = await PowerCmd.Apply<VulnerablePower>(
            choiceContext, base.Owner.Creature, DynamicVars.Vulnerable.IntValue, base.Owner.Creature, this, silent: false);

        // 原版规则：给玩家施加的 Debuff 首次衰减会被跳过（SkipNextDurationTick = true），
        // 而持续型 Debuff 只在敌方回合结束时衰减，于是自身易伤会白白多持续一整个敌方回合
        // （打出后首个敌方回合结束时不减层）。这里显式取消跳过，使它在下一个敌方回合正常衰减。
        if (vulnerable != null)
        {
            vulnerable.SkipNextDurationTick = false;
        }

        // 能力：每回合额外抽1/2张牌，重复打出可叠加
        await PowerCmd.Apply<MegalovaniaPower>(
            choiceContext, base.Owner.Creature, DynamicVars.Cards.IntValue, base.Owner.Creature, this, silent: false);
    }

    // 升级后的效果逻辑：额外抽牌1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
