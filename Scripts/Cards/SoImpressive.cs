using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

/// <summary>复制并自动打出自己的上一张牌；音效在复制品完整结算后播放。</summary>
[RegisterCard(typeof(ShuHanCardPool))]
public class SoImpressive : NewsanguoCardTemplate
{
    private int _fallbackIndex = -1;
    private CardModel? _copiedTarget;
    private string? _playedTitle;
    private bool _historyPreview;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png");
    public SoImpressive() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    private CardModel? PreviousCard
    {
        get
        {
            CardModel? previous = CombatManager.Instance.History.CardPlaysFinished
                .LastOrDefault(entry => entry.CardPlay.Card.Owner == Owner)?.CardPlay.Card;
            // 同类牌视作没有可复制的历史，统一使用本牌已确定的衍生牌占位。
            return previous is SoImpressive ? null : previous;
        }
    }
    private CardModel FallbackTemplate => _fallbackIndex switch
    {
        1 => ModelDb.Card<Ivory>(),
        2 => ModelDb.Card<Card1225>(),
        _ => ModelDb.Card<OttoMother>()
    };
    private CardModel DisplaySource => IsDupe && _copiedTarget is not null
        ? _copiedTarget : PreviousCard ?? FallbackTemplate;

    private static string SourceName(CardModel source) => source is SoImpressive impressive
        ? impressive._playedTitle ?? impressive.TitleLocString.GetFormattedText() : source.Title;
    private string DynamicTitle(string name)
    {
        LocString title = new("cards", "NEWSANGUO_CARD_SO_IMPRESSIVE.combatTitle");
        title.Add("PreviousCard", name);
        return title.GetFormattedText() + (IsUpgraded ? "+" : "");
    }
    public override string Title
    {
        get
        {
            if (_historyPreview) return _playedTitle ?? base.Title;
            if (!IsMutable || !IsInCombat || CombatState is null) return base.Title;
            if (Pile?.Type == PileType.Play && _playedTitle is not null) return _playedTitle;
            return DynamicTitle(SourceName(DisplaySource));
        }
    }
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (_historyPreview || !IsMutable || !IsInCombat || CombatState is null)
                return System.Array.Empty<IHoverTip>();
            CardModel source = DisplaySource;
            // 预览克隆不注册进战斗状态；同类牌的预览冻结名称且不继续递归生成提示。
            if (source is SoImpressive)
            {
                var preview = (SoImpressive)source.MutableClone();
                preview._historyPreview = true;
                source = preview;
            }
            return [HoverTipFactory.FromCard(source)];
        }
    }
    private void EnsureFallback()
    {
        if (_fallbackIndex < 0)
            _fallbackIndex = Owner.RunState.Rng.CombatCardGeneration.NextInt(3);
    }
    private void RefreshHandTitle()
    {
        if (Pile?.Type == PileType.Hand)
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this) EnsureFallback();
        return Task.CompletedTask;
    }
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card == this) { EnsureFallback(); RefreshHandTitle(); }
        return Task.CompletedTask;
    }
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return Task.CompletedTask;
        EnsureFallback();
        // 自动打出的同类复制品沿用原牌当时的目标，不能重新指向当前历史中的自身。
        if (!IsDupe || _copiedTarget is null)
        {
            CardModel? previous = PreviousCard;
            _playedTitle = DynamicTitle(SourceName(previous ?? FallbackTemplate));
            _copiedTarget = previous ?? (_fallbackIndex switch
            {
                1 => CombatState!.CreateCard<Ivory>(Owner),
                2 => CombatState!.CreateCard<Card1225>(Owner),
                _ => CombatState!.CreateCard<OttoMother>(Owner)
            });
        }
        return Task.CompletedTask;
    }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_copiedTarget is not null)
        {
            // 与History Course相同：保留升级、附魔状态，自动选择合法目标，不支付复制品能量。
            using (var playback = NewsanguoSfx.CapturePlayback())
            {
                await CardCmd.AutoPlay(choiceContext, _copiedTarget.CreateDupe(), null);
                await playback.WaitAsync(leadSeconds: 0.5);
            }
        }
        NewsanguoSfx.Play("event:/newsanguo/sfx/so_impressive");
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        RefreshHandTitle();
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
