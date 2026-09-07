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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class QuadBlast : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每次打击造成 4 点伤害，攻击 4 次
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(4, ValueProp.Move),
        new RepeatVar(4)
    ];

    public QuadBlast() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 四段“先喊后砍”循环：
        // 播放第 i 段语音（不等待结束）→ 立刻造成第 i 段伤害；
        // 前 3 段：本段语音播完后才进入下一段（保证各段语音不重叠）；
        // 第 4 段：语音播放后不再等待，直接收尾。
        for (int i = 1; i <= 4; i++)
        {
            // 开始播放第 i 段语音
            var player = NewsanguoSfx.Play($"event:/newsanguo/sfx/quad_blast_{i}");

            // 造成一段伤害（对全体敌人打击一次）
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingAllOpponents(combatState)
                .Execute(choiceContext);

            // 前 3 段等本段语音播完，第 4 段无需等待
            if (i < 4)
            {
                await NewsanguoSfx.WaitFinishedAsync(player);
            }
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 每次打击伤害从 4 提高到 5
        DynamicVars.Damage.UpgradeValueBy(1);
    }
}
