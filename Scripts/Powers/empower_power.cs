using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;
namespace newsanguo.Scripts.Powers;

// 注册能力到游戏
[RegisterPower]
public class empower_power : ModPowerTemplate
{
    private class Data
    {
        // 记录的牌（能力刚施加、尚未记录时可能为 null）
        public CardModel? recordedCard;
    }

    private const string CardKey = "Card";

    // 能力类型：正面 Buff
    public override PowerType Type => PowerType.Buff;
    // 非叠加状态：不在右下角显示层数数字
    public override PowerStackType StackType => PowerStackType.Single;
    // 不允许负数
    public override bool AllowNegative => false;
    // 每次打出“赋值”都是独立实例，各自记录一张牌
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    // 需要每回合抽牌后的战斗钩子
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 显示记录的牌名
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new StringVar(CardKey)
    ];

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 记录一张牌（保留升级状态），并在能力描述中显示其名称
    public void SetSelectedCard(CardModel card)
    {
        CardModel clone = card.CreateClone();
        CardCmd.ClearAffliction(clone);
        GetInternalData<Data>().recordedCard = clone;
        ((StringVar)DynamicVars[CardKey]).StringValue = clone.Title;
    }

    // 每回合抽牌后（自动出牌阶段之前），选择一张手牌变化为记录的牌
    public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner is null || player != Owner.Player)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.recordedCard is null)
        {
            return;
        }

        // 手牌为空时无事发生
        if (PileType.Hand.GetPile(player).Cards.Count == 0)
        {
            return;
        }

        // 触发“赋值”音效（对应 FMOD 事件 event:/newsanguo/sfx/empower_power）
        NewsanguoSfx.Play("event:/newsanguo/sfx/empower_power");

        // 选择一张手牌变化为记录的牌：提示文案中直接告知会变化成什么牌
        LocString prompt = new LocString("cards", "NEWSANGUO_CARD_SELECT_TRANSFORM_TO");
        prompt.Add("Target", data.recordedCard.Title);
        List<CardModel> selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(prompt, 1),
            context: choiceContext,
            player: player,
            filter: null,
            source: this)).ToList();
        CardModel? original = selected.FirstOrDefault();
        if (original is null)
        {
            return;
        }

        ICombatState? combatState = CombatState;
        if (combatState is null)
        {
            return;
        }

        // 以记录的牌（保持升级状态）替换所选手牌
        CardModel canonical = ModelDb.GetById<CardModel>(data.recordedCard.Id);
        CardModel replacement = combatState.CreateCard(canonical, player);
        if (data.recordedCard.IsUpgraded)
        {
            CardCmd.Upgrade(replacement);
        }
        await CardCmd.Transform(original, replacement);
    }
}
