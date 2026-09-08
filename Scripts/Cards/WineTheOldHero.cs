using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Powers;
using wine_the_old_hero_power = newsanguo.Scripts.Powers.WineTheOldHero;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class WineTheOldHero : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每失去 1 点酒力获得的格挡（升级后 2）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<wine_the_old_hero_power>("wine_the_old_hero", 1)
    ];

    // 鼠标悬停时显示格挡与酒力提示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<DrunkenMightPower>()
    ];
    
    // 鼠标悬停时显示格挡提示
    public override bool GainsBlock => true;

    public WineTheOldHero() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/wine_the_old_hero");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得酒是老英雄能力
        int powerAmount = DynamicVars["wine_the_old_hero"].IntValue;
        await PowerCmd.Apply<wine_the_old_hero_power>(
            choiceContext,
            base.Owner.Creature,
            powerAmount,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 每失去 1 点酒力获得的格挡从 1 提高到 2 (1+1)
        DynamicVars["wine_the_old_hero"].UpgradeValueBy(1);
    }
}
