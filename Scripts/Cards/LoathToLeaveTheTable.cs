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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class LoathToLeaveTheTable : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：对所有敌人造成 25 点伤害；酒力阈值 10（升级后 8）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(25, ValueProp.Move),
        new IntVar("wine_threshold", 10)
    ];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    // 酒力超过阈值时金色高亮（提示击晕与弃牌效果会触发）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            DrunkenMightPower? wine = base.Owner.Creature.GetPower<DrunkenMightPower>();
            return wine is not null && wine.Amount >= DynamicVars["wine_threshold"].IntValue;
        }
    }

    // 出牌语音完整时长（秒），取自 audios/loath_to_leave_the_table.wav（2.2s）。
    // 酒力充足触发“掀桌”特效前先等语音播完；替换语音文件后需同步校准本值。
    private const float VoiceLineDurationSeconds = 2.2f;

    public LoathToLeaveTheTable() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState!;

        // 打出此牌时快照酒力：判定必须在打出攻击牌后的酒力减半（AfterCardPlayed）之前，
        // 且不受伤害执行期间任何酒力变动的影响，因此先取快照再执行伤害。
        int wineAmount = base.Owner.Creature.GetPower<DrunkenMightPower>()?.Amount ?? 0;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/loath_to_leave_the_table");

        // 播放角色施法动画（与出牌语音并行）
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 对所有敌人造成 25 点伤害
        async Task DealDamage()
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(combatState)
                .Execute(choiceContext);
        }

        // 若你的酒力不小于阈值（基础 10，升级 8）：
        int wineThreshold = DynamicVars["wine_threshold"].IntValue;
        if (wineAmount >= wineThreshold)
        {
            // 先等出牌语音播完（语音 2.2s，施法动画已先行消耗 CastAnimDelay），
            // 再播放掀桌音效、造成伤害并击晕，避免特效盖过语音。
            await Cmd.Wait(VoiceLineDurationSeconds - base.Owner.Character.CastAnimDelay);

            // 播放掀桌音效
            NewsanguoSfx.Play("event:/newsanguo/sfx/loath_to_leave_the_table_damage");

            await DealDamage();

            // 击晕所有敌人
            foreach (Creature enemy in combatState.GetOpponentsOf(base.Owner.Creature).Where(c => c.IsAlive))
            {
                await CreatureCmd.Stun(enemy);
            }

            // 所有玩家丢弃所有手牌
            foreach (Creature player in combatState.GetTeammatesOf(base.Owner.Creature).Where(c => c.IsPlayer && c.IsAlive))
            {
                await CardCmd.Discard(choiceContext, PileType.Hand.GetPile(player.Player).Cards);
            }
        }
        else
        {
            // 酒力不足：立即造成伤害（伤害与出牌音效同时进行）
            await DealDamage();
        }
    }

    // 升级后的效果逻辑：伤害 25 → 35；酒力阈值 10 → 8
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10);
        DynamicVars["wine_threshold"].UpgradeValueBy(-2);
    }
}
