using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册到诅咒卡池；随机生成禁用，仍由天意之力机制定向给予。
[RegisterCard(typeof(CurseCardPool))]
public class HeavensDecay : NewsanguoCurseTemplate
{
    public override bool CanBeGeneratedByModifiers => false;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public override bool IsEmberCard => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal,
        CardKeyword.Sly,
        CardKeyword.Eternal,
        .. base.CanonicalKeywords
    ];

    // 悬停提示：牌名即“天意侵蚀”，而天意之力的说明文本中会出现“天意侵蚀”，两者须成对展示
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HeavensForce.HoverTip(),
        HoverTipFactory.FromPower<HeavensDecayPower>()
    ];

    // 属于“天意”体系（涉及天意之力/天意侵蚀）
    public override bool IsHeavensCard => true;

    private bool _resolvingEmber;

    public HeavensDecay() : base(0)
    {
    }

    // 正常打出、奇巧和余烬自动打出均在此处结算相同效果。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/heavens_decay");
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await HeavensForce.Set(choiceContext, Owner, 0, this);
        await PowerCmd.Apply<HeavensDecayPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (card != this || _resolvingEmber)
            return;
        _resolvingEmber = true;
        try
        {
            // 自动打出被消耗的本体；防止外部强制消耗规则造成嵌套重入。
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
        finally
        {
            _resolvingEmber = false;
        }
    }
}
