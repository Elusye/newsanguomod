using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
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

    // 卡牌基础数值：对所有敌人造成 25 点伤害；酒力阈值 6（升级后 4）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(25, ValueProp.Move),
        new IntVar("WineThreshold", 6)
    ];

    // 悬停提示：展示“酒力”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DrunkenMightPower>()];

    // 酒力超过阈值时金色高亮（提示击晕与弃牌效果会触发）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            DrunkenMightPower? wine = base.Owner.Creature.GetPower<DrunkenMightPower>();
            return wine is not null && wine.Amount >= DynamicVars["WineThreshold"].IntValue;
        }
    }

    // 出牌语音时长 2.2s（audios/loath_to_leave_the_table.wav）；聚光灯蓄势时长 1.625s。
    // 语音先播 0.6s 起头，再启动聚光灯，两者并行：0.6 + 1.625 ≈ 2.2s，冲击落下时语音刚好播完。
    // 替换语音文件或调整蓄势时长后需同步校准本值。
    private const float SpotlightLeadInSeconds = 0.6f;

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

        // 对所有敌人造成 25 点伤害；掀桌分支使用“华丽终幕”同款冲击特效与钝击音效
        async Task DealDamage(bool grandFinale)
        {
            var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(combatState);

            if (grandFinale)
            {
                attack.WithHitVfxNode(NGrandFinaleImpactVfx.Create)
                    .WithHitFx(null, null, "blunt_attack.mp3");
            }
            else
            {
                attack.WithHitFx("vfx/vfx_attack_slash");
            }

            await attack.Execute(choiceContext);
        }

        // 若你的酒力不小于阈值（基础 6，升级 4）：
        int wineThreshold = DynamicVars["WineThreshold"].IntValue;
        if (wineAmount >= wineThreshold)
        {
            // 语音播 0.6s 起头后启动聚光灯，不等语音播完（两者并行）。
            await Cmd.Wait(SpotlightLeadInSeconds);

            // 聚光灯蓄势特效（同原版“华丽终幕”），蓄势结束时语音刚好播完，随即落下伤害。
            NGrandFinaleVfx? grandFinaleVfx = NGrandFinaleVfx.Create(base.Owner.Creature);
            if (grandFinaleVfx != null)
            {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(grandFinaleVfx);
                await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
            }

            // 播放掀桌音效
            NewsanguoSfx.Play("event:/newsanguo/sfx/loath_to_leave_the_table_damage");

            await DealDamage(grandFinale: true);

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
            await DealDamage(grandFinale: false);
        }
    }

    // 升级后的效果逻辑：伤害 25 → 35；酒力阈值 6 → 4
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10);
        DynamicVars["WineThreshold"].UpgradeValueBy(-2);
    }
}
