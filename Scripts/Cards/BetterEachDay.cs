using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class BetterEachDay : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // X 费牌（同原版旋风斩/天际钻头）：打出时自动花费全部剩余能量
    protected override bool HasEnergyCostX => true;

    // 能量图标显示用（与“他过江我也过江”一致：{Energy:energyIcons()}）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1)
    ];

    // 消耗（升级后移除）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public BetterEachDay() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/better_each_day");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // X = 本回合为打出此牌花费的能量
        int x = ResolveEnergyXValue();

        // 下一回合抽 X+1 张牌、获得 X+1 点能量（原版下回合能力）
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, base.Owner.Creature, x + 1, base.Owner.Creature, this);
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, base.Owner.Creature, x + 1, base.Owner.Creature, this);
    }

    // 升级：去除消耗
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    // 降级：恢复“消耗”
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Exhaust);
    }
}
