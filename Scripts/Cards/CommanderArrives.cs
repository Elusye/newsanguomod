using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class CommanderArrives : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 打出时获得格挡（卡面显示格挡数值）
    public override bool GainsBlock => true;

    // 卡牌基础数值：获得 6 点格挡；将 1（升级 2）张军杖加入手牌
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(6m, ValueProp.Move),
        new CardsVar(1)
    ];

    // 悬停提示：展示军杖卡面说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<MilitaryCudgel>()
    ];

    public CommanderArrives() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/commander_arrives");

        // 获得 6 点格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block, cardPlay, fast: false);

        // 将 1（2）张军杖加入手牌，逐张加入并留出间隔
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            await MilitaryCudgel.CreateInHand(base.Owner, combatState);
            await Cmd.Wait(0.25f);
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 军杖张数从 1 提高到 2
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
