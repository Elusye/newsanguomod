using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Combat.Rewards;

using newsanguo.Scripts.Patches;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Rewards;

/// <summary>
/// 「创造模式」奖励：战斗奖励屏上多出一张奖励，点选后从游戏里的全部卡牌中自选一张加入牌组。
///
/// 为什么做成“奖励”而不是战斗结束直接弹窗：
/// 原版给战斗结束收益的正统做法就是额外奖励（参考 ForbiddenGrimoirePower → room.AddExtraReward），
/// 这样它会出现在奖励屏上、可见也可跳过，而且随房间存读档。
///
/// 原版 Reward 的 RewardType 是封闭枚举、静态工厂 Reward.FromSerializable 对未知类型会抛异常，
/// 所以自造 Reward 必须走 RitsuLib 的 ModCustomReward + ModRewardRegistry：
/// 注册时 RitsuLib 会为本奖励铸造一个动态 RewardType，并在存读档时按它重建本类型。
///
/// 描述文字放在 localization/*/cards.json 的 NEWSANGUO_CARD_CREATIVE_MODE_REWARD。
/// </summary>
public sealed class CreativeModeReward : ModCustomReward
{
    // 选牌界面底部的提示文字（与卡牌共用同一个键）
    private static readonly LocString SelectionPrompt = new("cards", "NEWSANGUO_CARD_CREATIVE_MODE_PROMPT");

    public CreativeModeReward(Player player)
        : base(player)
    {
    }

    // 由注册时铸造的动态奖励类型决定；未注册时读取会先触发注册
    public override RewardType ModRewardType => CreativeModeRewardRegistration.RewardType;

    // 描述走 mod 自己的 cards 表，避免依赖原版 gameplay_ui 表
    protected override string DescriptionLocTable => "cards";

    protected override string DescriptionLocKey => "NEWSANGUO_CARD_CREATIVE_MODE_REWARD";

    // 复用原版“特殊卡牌奖励”图标；文件缺失时 RitsuLib 只会画一个空图标，不会报错
    protected override string? RewardIconPath => "res://images/ui/reward_screen/reward_icon_special_card.png";

    // 奖励内容在点选时才生成，这里没有需要预先填充的东西
    public override void MarkContentAsSeen()
    {
    }

    protected override async Task<bool> OnSelect()
    {
        Player player = base.Player;

        // 候选 = 游戏里的全部卡牌，只排除「创造模式」自己
        string selfEntry = ModelDb.GetId(typeof(CreativeMode)).Entry;
        List<CardCreationResult> options = [];
        foreach (CardModel canonical in ModelDb.AllCards)
        {
            if (string.Equals(canonical.Id.Entry, selfEntry, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                // 造一张属于该玩家的可变实例（选牌屏要求候选牌已有 owner）
                options.Add(new CardCreationResult(player.RunState.CreateCard(canonical, player)));
            }
            catch (Exception exception)
            {
                // 个别牌造不出来（例如被其它 mod 改坏的原型）时跳过，不影响整局
                GD.PrintErr($"CreativeModeReward: failed to create {canonical.Id.Entry}: {exception.Message}");
            }
        }

        if (options.Count == 0)
        {
            GD.PrintErr("CreativeModeReward: no candidate cards to offer. Skipping to prevent softlock.");
            return false;
        }

        // 让「创造模式」这一屏的选牌界面带上图鉴式的搜索 / 筛选
        // （补丁只在消费到这个标记时装配，原版别的选牌屏不受影响）
        CreativeModePickerSearch.Request();

        IEnumerable<CardModel> selected;
        try
        {
            selected = await CardSelectCmd.FromSimpleGridForRewards(
                new BlockingPlayerChoiceContext(),
                options,
                player,
                new CardSelectorPrefs(SelectionPrompt, 1));
        }
        finally
        {
            // 兜底：没真正弹出选牌屏（例如联机里由远端玩家抉择）时清掉残留标记，
            // 免得之后某个原版选牌屏被误加上搜索栏
            CreativeModePickerSearch.Clear();
        }

        CardModel? chosen = selected.FirstOrDefault();

        // 回收没被选中的临时实例（选中的那张要真的进牌组，不能回收）
        foreach (CardCreationResult option in options)
        {
            if (!ReferenceEquals(option.Card, chosen))
            {
                player.RunState.RemoveCard(option.Card);
            }
        }

        if (chosen is null)
        {
            return false;
        }

        // 加入牌组（会照常走 Hook.ShouldAddToDeck / ModifyCardBeingAddedToDeck）
        CardPileAddResult result = await CardPileCmd.Add(chosen, PileType.Deck);
        if (result.success)
        {
            CardCmd.PreviewCardPileAdd(result, 2f);
        }

        return result.success;
    }
}

/// <summary>
/// 把「创造模式」奖励注册进 RitsuLib 的自定义奖励表。
/// 必须在 mod 初始化时调用一次（见 <see cref="Entry.Init" />），否则存读档时无法还原本奖励。
/// </summary>
internal static class CreativeModeRewardRegistration
{
    /// <summary>注册用的本地奖励名（RitsuLib 会加上 mod 前缀铸造动态 RewardType）。</summary>
    internal const string RewardStem = "creative_mode_reward";

    private static RewardType? _rewardType;

    /// <summary>本奖励的动态奖励类型；未注册时读取会先触发注册。</summary>
    internal static RewardType RewardType
    {
        get
        {
            Register();
            return _rewardType!.Value;
        }
    }

    /// <summary>注册本奖励（重复调用无副作用）。</summary>
    internal static void Register()
    {
        if (_rewardType.HasValue)
        {
            return;
        }

        ModRewardDefinition definition = ModRewardRegistry
            .For(Entry.ModId)
            .RegisterOwned(RewardStem, static (save, player, json) => new CreativeModeReward(player));

        _rewardType = definition.RewardType;
        Entry.Logger.Info($"[创造模式] 已注册自定义奖励 {definition.Id}（RewardType=0x{(int)definition.RewardType:X8}）");
    }
}
