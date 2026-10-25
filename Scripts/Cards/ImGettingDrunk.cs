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

using newsanguo.Scripts.Characters;
using MegaCrit.Sts2.Core.Models.CardPools;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 原曹魏实体保留供存档和生成效果使用，不再进入曹魏卡池。
[RegisterCard(typeof(TokenCardPool))]
public class ImGettingDrunk : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：立即获得的酒力（升级后 8）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DrunkenMightPower>(6m)
    ];

    // 悬停提示：展示“酒力”说明 + “不胜酒力”卡面说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HoverTipFactory.FromCard<Lightweight>()
    ];

    public ImGettingDrunk() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/im_getting_drunk");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 立即获得 6（8）点酒力
        await PowerCmd.Apply<DrunkenMightPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["DrunkenMightPower"].IntValue,
            Owner.Creature,
            this);

        // 负面效果：将一张“不胜酒力”加入你的手牌
        await Lightweight.CreateInHand(Owner, CombatState!);
    }

    // 升级：酒力 6 → 8
    protected override void OnUpgrade()
    {
        DynamicVars["DrunkenMightPower"].UpgradeValueBy(2);
    }
}
