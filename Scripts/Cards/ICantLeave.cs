using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Cards;

/// <summary>99费诅咒：打出后返回手牌；被消耗时通过余烬自动打出本体。</summary>
[RegisterCard(typeof(CurseCardPool))]
public class ICantLeave : NewsanguoCurseTemplate
{
    public override bool IsEmberCard => true;
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Innate,
        CardKeyword.Retain,
        CardKeyword.Sly,
        CardKeyword.Eternal,
        .. base.CanonicalKeywords
    ];
    private bool _resolvingEmber;
    public ICantLeave() : base(99) { }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NewsanguoSfx.Play("event:/newsanguo/sfx/i_cant_leave");
        return Task.CompletedTask;
    }

    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation location = base.GetResultLocationForCardPlay();
        // 使用引擎出牌去向返回手牌，保留复制品及外部强制消耗的处理。
        if (location.pileType == PileType.Discard)
        {
            location.pileType = PileType.Hand;
            location.position = CardPilePosition.Bottom;
        }
        return location;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this || _resolvingEmber)
            return;
        _resolvingEmber = true;
        try
        {
            await CardCmd.AutoPlay(choiceContext, this, null);
        }
        finally
        {
            _resolvingEmber = false;
        }
    }
}
