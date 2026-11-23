using newsanguo.Scripts.Patches;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(ShuHanCardPool))]
public class DeafenMe : NewsanguoCardTemplate
{
    public override bool IsTigerDragonCard => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new OmenVar(10m),
        new DynamicVar("ExtraOmen", 4m),
        new PowerVar<DeafenMePower>(10m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<DragonOmenPower>(), HoverTipFactory.FromPower<DeafenMePower>()];


    public DeafenMe() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    private sealed class OmenVar(decimal amount) : PowerVar<DragonOmenPower>(amount)
    {
        public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode,
            Creature? target, bool runGlobalHooks)
        {
            decimal total = card is DeafenMe deafen ? deafen.GetOmenAmount() : BaseValue;
            PreviewValue = runGlobalHooks && card.CombatState is { } combat
                ? Hook.ModifyPowerAmountGiven(combat, ModelDb.Power<DragonOmenPower>(),
                    card.Owner.Creature, total, target, card, out _)
                : total;
        }
    }

    private int GetOmenAmount()
    {
        int debuffs = IsMutable && IsInCombat && Owner.PlayerCombatState is { } state
            ? ZilongDebuffHistoryPatch.CountFor(state, CombatState!) : 0;
        return DynamicVars["DragonOmenPower"].IntValue + debuffs * DynamicVars["ExtraOmen"].IntValue;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this)
            RefreshOmenPreview();
        return Task.CompletedTask;
    }

    internal void RefreshOmenPreview()
    {
        if (Owner.PlayerCombatState is not { } state)
            return;
        ZilongDebuffHistoryPatch.Track(state, this);
        if (Pile?.Type == PileType.Hand)
            NCard.FindOnTable(this)?.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 播放出牌音效（静音前的最后一声）
        NewsanguoSfx.Play("event:/newsanguo/sfx/deafen_me");

        int omen = GetOmenAmount();
        await PowerCmd.Apply<DragonOmenPower>(choiceContext, cardPlay.Target, omen, Owner.Creature, this);
        await PowerCmd.Apply<DeafenMePower>(choiceContext, Owner.Creature,
            DynamicVars["DeafenMePower"].IntValue, Owner.Creature, this);
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        DynamicVars["DragonOmenPower"].UpgradeValueBy(4m);
        DynamicVars["ExtraOmen"].UpgradeValueBy(2m);
    }
}
