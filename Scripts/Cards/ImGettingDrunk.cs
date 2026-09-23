using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class ImGettingDrunk : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：立即获得的酒力（升级 11）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>(8m)
    ];

    // 悬停提示：展示“酒力”说明 + “不胜酒力”卡面说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HoverTipFactory.FromCard<Lightweight>()
    ];

    public ImGettingDrunk() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/im_getting_drunk");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 立即获得 8（11）点酒力
        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["DrunkenMightPower"].IntValue,
            Owner.Creature,
            this);

        // 负面效果：将一张“不胜酒力”加入你的弃牌堆
        await Lightweight.CreateInDiscard(Owner, CombatState!);
    }

    // 升级：酒力 8 → 11
    protected override void OnUpgrade()
    {
        DynamicVars["DrunkenMightPower"].UpgradeValueBy(3);
    }
}
