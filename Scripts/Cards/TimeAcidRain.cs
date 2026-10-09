using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池（曹魏新增）
[RegisterCard(typeof(NewsanguoCardPool))]
public class TimeAcidRain : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 词条：消耗
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    // 卡牌基础数值：失去 3 点天意之力（升级后 2 点）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(3m)
    ];

    // 悬停提示：展示“天意之力”“天意侵蚀”与“格挡”说明
    // （描述中会提到“天意之力”，两者须成对展示；描述里提到格挡，故按原版 Expose 的惯例附上格挡说明）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block)
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public TimeAcidRain() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    // 升级效果：失去的天意之力从 3 减少到 2
    protected override void OnUpgrade()
    {
        DynamicVars["HeavensForcePower"].UpgradeValueBy(-1);
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/time_acid_rain");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 失去天意之力
        await HeavensForce.Add(choiceContext, base.Owner, -DynamicVars["HeavensForcePower"].IntValue, this);

        // 去除敌方所有格挡 + 清除敌方所有正面效果
        // （去格挡的写法参考原版卡牌「暴露」Expose：CreatureCmd.LoseBlock(choiceContext, target, target.Block, remover)）
        // 正面效果只清“按当前数值算正面”的能力
        // （力量、灵巧这类可负计数能力为负值时是负面效果，不该被这张牌带走）
        // 例外：不清「流沙」SandpitPower——它的 AfterRemoved 会强杀被卷入的玩家
        // （.decompile/sts2full/sts2.decompiled.cs:104573-104606，其中 :104603 是
        //  `await CreatureCmd.Kill(allAffectedCreature2, force: true);`）⇒ 清掉它等于直接自杀
        // 先快照再逐个移除，避免遍历途中集合变化
        foreach (Creature enemy in combatState.GetOpponentsOf(base.Owner.Creature).Where(c => c.IsAlive))
        {
            // 格挡清零（LoseBlock 内部已判 amount<=0、目标已死、战斗收尾，这里不用再判）
            await CreatureCmd.LoseBlock(choiceContext, enemy, enemy.Block, base.Owner.Creature);

            List<PowerModel> buffs = enemy.Powers
                .Where(p => p.TypeForCurrentAmount == PowerType.Buff && p is not SandpitPower)
                .ToList();
            foreach (PowerModel buff in buffs)
            {
                await PowerCmd.Remove(buff);
            }
        }
    }
}
