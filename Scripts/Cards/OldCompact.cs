using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 压缩（旧）：旧版本的压缩，获得格挡并将手牌中所有状态牌变化为燃料（旧）
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class OldCompact : NewsanguoCardTemplate
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<DefectCardPool>();

    // 获得格挡：可被灵巧等格挡附魔识别
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的格挡
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(6, ValueProp.Move)
    ];

    // 鼠标悬停时显示“燃料（旧）”卡牌标注（升级时显示升级版燃料）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<OldFuel>(IsUpgraded)
    ];

    public OldCompact() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑（参考原版压缩 Compact）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, false);

        // 将手牌中所有状态牌变化为燃料（旧）（升级后为燃料（旧）+）
        CardPile hand = PileType.Hand.GetPile(base.Owner);
        foreach (CardModel statusCard in hand.Cards.Where(card => card.Type == CardType.Status).ToList())
        {
            CardModel fuel = combatState.CreateCard<OldFuel>(base.Owner);
            if (IsUpgraded)
            {
                CardCmd.Upgrade(fuel);
            }

            await CardCmd.Transform(statusCard, fuel);
        }
    }

    // 升级：获得的格挡 6 → 7
    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1);
    }
}
