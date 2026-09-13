using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class HeavenlyTroops : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 悬停提示：展示“士兵”卡牌标注（升级时显示升级版士兵）、天意之力与天意侵蚀说明
    // （描述中会提到“天意之力”，两者须成对展示）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<Soldier>(IsUpgraded),
        HoverTipFactory.FromPower<HeavensForcePower>(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 卡牌基础数值：经过 2 个回合结束后发放 5 张士兵（turn_delay 需与 heavenly_troops_power 的倒计时保持同步）；
    // 打出时失去 3 点天意之力（升级后 2 点）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<HeavensForcePower>("heavens_force", 3),
        new IntVar("soldier_count", 5),
        new IntVar("turn_delay", 2)
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    public HeavenlyTroops() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 升级效果：失去的天意之力从 3 减少到 2
    protected override void OnUpgrade()
    {
        DynamicVars["heavens_force"].UpgradeValueBy(-1);
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/heavenly_troops");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 失去天意之力
        int lostAmount = DynamicVars["heavens_force"].IntValue;
        await PowerCmd.Apply<HeavensForcePower>(
            choiceContext,
            base.Owner.Creature,
            -lostAmount,
            base.Owner.Creature,
            this,
            silent: false);

        // 附加“天降雄兵”能力：经过 2 次玩家回合结束后，将对应数量的士兵加入手牌。
        // 升级后改为“天降雄兵+”，发放升级版“士兵+”。
        // 同一回合内打出多次会叠加士兵数量并重置倒计时；不同回合打出的各自独立倒计时。
        int soldierCount = DynamicVars["soldier_count"].IntValue;
        int turnNumber = base.Owner.PlayerCombatState!.TurnNumber;
        if (IsUpgraded)
        {
            HeavenlyTroopsPlusPower? existingPlus = base.Owner.Creature
                .GetPowerInstances<HeavenlyTroopsPlusPower>()
                .FirstOrDefault(p => p.IsFromTurn(turnNumber));
            if (existingPlus != null)
            {
                await PowerCmd.ModifyAmount(choiceContext, existingPlus, soldierCount, base.Owner.Creature, this);
                existingPlus.ResetTurnsLeft();
            }
            else
            {
                HeavenlyTroopsPlusPower? plusPower = await PowerCmd.Apply<HeavenlyTroopsPlusPower>(
                    choiceContext,
                    base.Owner.Creature,
                    soldierCount,
                    base.Owner.Creature,
                    this);
                plusPower?.SetTurnNumber(turnNumber);
            }
        }
        else
        {
            HeavenlyTroopsPower? existing = base.Owner.Creature
                .GetPowerInstances<HeavenlyTroopsPower>()
                .FirstOrDefault(p => p.IsFromTurn(turnNumber));
            if (existing != null)
            {
                await PowerCmd.ModifyAmount(choiceContext, existing, soldierCount, base.Owner.Creature, this);
                existing.ResetTurnsLeft();
            }
            else
            {
                HeavenlyTroopsPower? power = await PowerCmd.Apply<HeavenlyTroopsPower>(
                    choiceContext,
                    base.Owner.Creature,
                    soldierCount,
                    base.Owner.Creature,
                    this);
                power?.SetTurnNumber(turnNumber);
            }
        }
    }
}
