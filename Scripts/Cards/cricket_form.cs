using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class cricket_form : NewsanguoCardTemplate
{
    // 基础耗能：3
    private const int energyCost = 3;
    // 卡牌类型：能力
    private const CardType type = CardType.Power;
    // 卡牌稀有度：稀有
    private const CardRarity rarity = CardRarity.Rare;
    // 目标类型：自身
    private const TargetType targetType = TargetType.Self;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的难以杀灭层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DynamicVar("HardToKillAmount", 2m)
    ];

    public cricket_form() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 悬停提示：展示“难以杀灭”正面效果的说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<HardToKillPower>()];

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        // 播放出牌音效
        SfxCmd.Play("event:/newsanguo/sfx/cricket_form");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 获得难以杀灭层数（上限999）
        int gain = DynamicVars["HardToKillAmount"].IntValue;
        HardToKillPower? existing = owner.Creature.GetPower<HardToKillPower>();
        if (existing is not null)
        {
            gain = System.Math.Min(gain, 999 - existing.Amount);
        }
        if (gain > 0)
        {
            await PowerCmd.Apply<HardToKillPower>(choiceContext, owner.Creature, gain, owner.Creature, this);
        }

        // 附加“蛐蛐形态”能力：回合开始时难以杀灭层数翻倍
        await PowerCmd.Apply<cricket_form_power>(choiceContext, owner.Creature, 1, owner.Creature, this);
    }

    // 升级：难以杀灭层数 -1（2 → 1）
    protected override void OnUpgrade()
    {
        DynamicVars["HardToKillAmount"].UpgradeValueBy(-1);
    }
}
