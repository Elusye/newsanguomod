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

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

/// <summary>
/// 「狂妄之人」（MEGALOVANIA，蜀汉专属）：0 费稀有[gold]能力牌[/gold]。
/// 打出时给自己 2 层[gold]易伤[/gold]（升级后 1 层，卡面用 <c>inverseDiff()</c> 显示为绿色），
/// 并挂上「狂妄之人」能力：之后每回合的能量上限 +1（实现方式参考「恭喜爹可以称帝了！」，见 <see cref="MegalovaniaPower"/>）。
/// 重复打出按层数叠加 —— 每层每回合 +1 能量，同时再叠一次代价易伤。
///
/// 卡图 res://newsanguo/images/cards/Megalovania.png；音效 event:/newsanguo/sfx/megalovania。
/// 卡面文案在 localization/*/cards.json 的 NEWSANGUO_CARD_MEGALOVANIA.*，
/// 能力文案在 localization/*/powers.json 的 NEWSANGUO_POWER_MEGALOVANIA_POWER.*。
/// </summary>
[RegisterCard(typeof(ShuHanCardPool))]
public class Megalovania : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：自身易伤 2 层（升级后 1 层）、每回合获得的能量 1（卡面用 {Energy:energyIcons()} 显示图标）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<VulnerablePower>(2m),
        new EnergyVar(1)
    ];

    // 0 费、稀有、能力牌、目标为自己
    public Megalovania() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 悬停提示：展示“易伤”关键词说明与能量提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.ForEnergy(this)
    ];

    // 打出时的效果逻辑：先给自己叠易伤（代价），再挂上「狂妄之人」能力（每回合 +1 能量）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/megalovania");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 代价：给自己叠易伤（升级后层数变少）
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext, base.Owner.Creature, DynamicVars.Vulnerable.IntValue, base.Owner.Creature, this, silent: false);

        // 能力：每回合的能量上限 +1（数值固定 1，叠加体现在能力层数上；被动修正见 MegalovaniaPower）
        await PowerCmd.Apply<MegalovaniaPower>(
            choiceContext, base.Owner.Creature, 1, base.Owner.Creature, this, silent: false);
    }

    // 升级后的效果逻辑：自身易伤 2 层 → 1 层（数值变低是加强，卡面用 inverseDiff 显示绿色）
    protected override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(-1m);
    }
}
