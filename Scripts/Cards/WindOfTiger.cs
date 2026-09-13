using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WindOfTiger : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：给予 4（升级 6）层“风从虎，云从龙”
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<WindOfTigerPower>(4m)
    ];

    // 悬停提示：展示“笑面虎”和“龙可是帝王之征啊”（升级后展示升级版）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<SmilingTiger>(IsUpgraded),
        HoverTipFactory.FromCard<DragonOmen>(IsUpgraded)
    ];
    // 构造函数
    public WindOfTiger() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        NewsanguoSfx.Play("event:/newsanguo/sfx/wind_of_tiger");

        // 获得 4（6）层“风从虎，云从龙”：层数即笑面虎额外获得的格挡与龙可是帝王之征啊额外给予的帝王之征层数
        int amount = DynamicVars["WindOfTigerPower"].IntValue;
        await PowerCmd.Apply<WindOfTigerPower>(
            choiceContext,
            base.Owner.Creature,
            amount,
            base.Owner.Creature,
            this,
            silent: false);

        // 将一张笑面虎和一张龙可是帝王之征啊加入手牌（升级后为升级版）
        NewsanguoSfx.Play("event:/newsanguo/sfx/wind_of_tiger_power");

        CardModel tiger = combatState.CreateCard<SmilingTiger>(base.Owner);
        CardModel dragon = combatState.CreateCard<DragonOmen>(base.Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(tiger);
            CardCmd.Upgrade(dragon);
        }

        await CardPileCmd.AddGeneratedCardToCombat(tiger, PileType.Hand, base.Owner, CardPilePosition.Random);
        await CardPileCmd.AddGeneratedCardToCombat(dragon, PileType.Hand, base.Owner, CardPilePosition.Random);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 层数从 4 提高到 6
        DynamicVars["WindOfTigerPower"].UpgradeValueBy(2);
    }
}
