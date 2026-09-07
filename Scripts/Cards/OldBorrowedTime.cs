using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 预借时间（旧）：旧版本的预借时间，0 费，给自己 3 层灾厄并获得能量
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class OldBorrowedTime : NewsanguoCardTemplate
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<NecrobinderCardPool>();

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的能量、给予自身的灾厄层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar(1),
        new PowerVar<DoomPower>("DoomPower", 3)
    ];

    // 悬停提示：展示“灾厄”关键词说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DoomPower>()
    ];

    public OldBorrowedTime() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, base.Owner);

        // 给予自身灾厄
        await PowerCmd.Apply<DoomPower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["DoomPower"].IntValue,
            base.Owner.Creature,
            this);
    }

    // 升级：获得的能量 1 → 2
    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}
