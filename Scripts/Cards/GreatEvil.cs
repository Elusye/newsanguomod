using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
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

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class GreatEvil : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 属于“天意”体系（涉及天意之力）
    public override bool IsHeavensCard => true;

    // 卡牌基础数值：对所有敌人造成 8（升级 12）点伤害；天意之力不大于 0 时获得 3 点天意之力
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8m, ValueProp.Move),
        new HeavensForceVar(3m)
    ];

    // 天意之力不大于 0 时金色高亮（提示打出这张牌会获得天意之力）
    protected override bool ShouldGlowGoldInternal => CanObtainForce;

    // 天意之力不大于 0；与 OnPlay 的结算条件共用同一处定义，避免两边漂移
    // （写法同 LightningStrike.CanObtainForce）
    private bool CanObtainForce => HeavensForce.Get(base.Owner) <= 0;

    // 悬停提示：展示“天意之力”与“天意侵蚀”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    public GreatEvil() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/great_evil");

        // 对所有敌人造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 天意之力不大于 0 时，获得 3 点天意之力
        if (CanObtainForce)
        {
            await HeavensForce.Add(choiceContext, base.Owner, DynamicVars["HeavensForcePower"].IntValue, this);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 8 提高到 12
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
