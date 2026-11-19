using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Patches;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

[RegisterCard(typeof(ShuHanCardPool))]
public class ZilongLocked : NewsanguoCardTemplate
{
    private int _discountApplied;
    private bool Unlocked => IsMutable && IsInCombat && EnergyCost.GetWithModifiers(CostModifiers.All) <= 1;
    private const string LockedArt = "res://newsanguo/images/cards/ZilongLocked.png";
    private const string UnlockedArt = "res://newsanguo/images/cards/ZhaoZilongOfChangshan.png";

    public override bool IsTigerDragonCard => true;

    public override CardAssetProfile AssetProfile => new(PortraitPath: LockedArt);
    public override string PortraitPath => Unlocked ? UnlockedArt : LockedArt;
    public override string Title => Unlocked
        ? new LocString("cards", "NEWSANGUO_CARD_ZILONG_LOCKED.unlockedTitle").GetFormattedText() +
            (IsUpgraded ? "+" : "")
        : base.Title;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new PowerVar<DragonOmenPower>(25m),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<DragonOmenPower>()];

    public ZilongLocked() : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this)
            RefreshDiscount();
        return Task.CompletedTask;
    }

    internal void RefreshDiscount()
    {
        if (Owner.PlayerCombatState is not { } state)
            return;

        int count = ZilongDebuffHistoryPatch.Track(state, this);
        int newlyGained = count - _discountApplied;
        if (newlyGained <= 0)
            return;

        _discountApplied = count;
        EnergyCost.AddThisCombat(-newlyGained * DynamicVars.Energy.IntValue);
        if (Pile?.Type == PileType.Hand)
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool unlocked = Unlocked;
        if (NewsanguoSfx.Play(unlocked
                ? "event:/newsanguo/sfx/zhao_zilong_of_changshan"
                : "event:/newsanguo/sfx/zilong_locked") is null)
        {
            NewsanguoSfx.Play(unlocked ? "event:/newsanguo/sfx/hail_king_of_hanzhong" : "event:/newsanguo/sfx/dragon_omen");
        }
        await PowerCmd.Apply<DragonOmenPower>(choiceContext, cardPlay.Target,
            DynamicVars["DragonOmenPower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DragonOmenPower"].UpgradeValueBy(8);
    }
}
