using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

/// <summary>
/// 天子（衍生技能，0 费）：获得 4 点天意之力（升级后 5 点）。
/// 这张牌获得的天意之力会随之减少 1（每次打出后 -1，最低 0）。
/// 数值只记在这张牌自己身上、**不做跨战斗保留**：每次战斗生成的都是新实例，从 4（升级后 5）点重新开始。
/// 卡面数字靠 setter 同步到 DynamicVars 的 BaseValue，因此打出后立刻刷新。
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public class SonOfHeaven : NewsanguoCardTemplate
{
    // 初始“获得的天意之力”
    private const int InitialGrant = 4;

    // 当前“获得的天意之力”（本场战斗内每次打出后 -1，最低 0；不写入存档）
    private int _grant = InitialGrant;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的天意之力（初始 4 点，之后由 Grant 的 setter 同步刷新）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(Grant)
    ];

    // 悬停提示：展示“天意之力”和“天意侵蚀”两个说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public SonOfHeaven() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    /// <summary>
    /// 当前这张牌能获得的天意之力（本场战斗内递减，最低 0；不写入存档，
    /// 每场战斗从 4 点、升级后从 5 点重新开始）。
    /// </summary>
    public int Grant
    {
        get => _grant;
        set
        {
            AssertMutable();
            _grant = value;
            DynamicVars["HeavensForcePower"].BaseValue = _grant;
        }
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（资源文件 son_of_heaven，响度补偿见 NewsanguoSfx.LoudnessGainDb）
        NewsanguoSfx.Play("event:/newsanguo/sfx/son_of_heaven");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得当前数值的天意之力
        await HeavensForce.Add(choiceContext, base.Owner, DynamicVars["HeavensForcePower"].IntValue, this);

        // 这张牌获得的天意之力减少 1（最低 0）
        ReduceGrant();
    }

    // 本次获得的天意之力 -1（最低 0），并刷新卡面数值
    private void ReduceGrant()
    {
        if (Grant > 0)
        {
            Grant -= 1;
        }
    }

    // 升级后的效果逻辑：获得的天意之力 4 → 5
    //
    // 注意原版 DynamicVar 没有独立的“升级位”：UpgradeValueBy 是直接加在 BaseValue 上的
    // （DynamicVar.cs:143-147），而本牌的数值就是“当前值”本身，所以这里必须让两处同步成同一个数，
    // 不能既 UpgradeValueBy(+1) 又让 setter 再 +1（否则会变成 6）。
    // 升级同时会标记“刚升级”，卡面数字因此照常显示为升级预览的绿色。
    protected override void OnUpgrade()
    {
        DynamicVars["HeavensForcePower"].UpgradeValueBy(1);
        // 此处 BaseValue 已是升级后的值，再走一次 setter 是幂等的，只为把 Grant 对齐
        Grant = (int)DynamicVars["HeavensForcePower"].BaseValue;
    }

    // 降级后的效果逻辑：DowngradeInternal 会用模板的 CanonicalVars 重建 DynamicVars（CardModel.cs:2135-2148），
    // 升级随之撤销、但 Grant 字段不会被重置，这里把它同步回重建后的值，避免“字段与卡面脱节”导致递减失效。
    protected override void AfterDowngraded()
    {
        Grant = (int)DynamicVars["HeavensForcePower"].BaseValue;
    }
}
