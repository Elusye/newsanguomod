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

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class Retire : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：获得的无实体层数
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<IntangiblePower>(1m)
    ];

    // 虚无 + 消耗（升级后仅移除虚无，保留消耗）
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    // 悬停提示：展示“无实体”说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];

    public Retire() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/retire");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 获得无实体层数
        await PowerCmd.Apply<IntangiblePower>(choiceContext, base.Owner.Creature, DynamicVars["IntangiblePower"].IntValue, base.Owner.Creature, this);

        // 结束你的回合（同原版 VoidForm）
        PlayerCmd.EndTurn(base.Owner, false, null);
    }

    // 升级：去除虚无，保留消耗
    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }

    // 降级：恢复“虚无”
    protected override void AfterDowngraded()
    {
        AddKeyword(CardKeyword.Ethereal);
    }
}
