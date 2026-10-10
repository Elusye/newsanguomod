using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Monsters;
namespace newsanguo.Scripts.Powers;

/// <summary>
/// “曹氏兵法”赋予的可叠加能力：层数（Amount）= 已打出的“曹氏兵法”张数。
/// 1) 你每打出一张牌，就以 1 生命值“募集”与层数相同数量的青州兵（这批青州兵在你的回合开始时失去）；
/// 2) 每当任何生物死亡，你获得与层数相同的力量（触发点同原版“忧郁”Melancholy 的 AfterDeath Hook）；
/// 3) 在你的回合开始时，移除本能力召唤的所有青州兵（这一批死亡不触发第 2 条）。
/// 参照原版“残影”（AfterimagePower）：打牌开始前记账层数，打出后按键结账并移除。
/// 因此“打牌开始时本能力还不存在”的那张牌（即首次打出“曹氏兵法”自身）不会触发；
/// 已有该能力后再打出“曹氏兵法”属于正常出牌，会照常触发。
/// 召唤物是本 mod 自己的怪物「青州兵」（Scripts/Monsters/QingzhouSoldier.cs），与奥斯提彻底区分开：
/// 不走原版 OstyCmd.Summon（那条路在已有奥斯提活着时只做 GainMaxHp：“一只、血量累加”），
/// 每次召唤都是独立个体（思路同创意工坊“召唤独立”SummonOstys，但只作用于本能力召唤的青州兵）；
/// 「古挽歌」等仍走原版路径召唤奥斯提，两者并存互不干扰。
/// </summary>
[RegisterPower]
public class CaoArtOfWarPower : ModPowerTemplate
{
    // 独立召唤的青州兵生命值固定为 1（需求：以 1 生命值独立召唤）
    private const decimal IndependentSoldierHp = 1m;

    // 每位玩家同时存活的募集青州兵最多 15 名，阵亡后可再次募集补足。
    private const int MaxLivingSoldiers = 15;

    // 账本：牌 → 那张牌开始打出时的层数。
    // 用 CardModel 作键（引用相等），精确到“这一张牌的这一次出牌”。
    private class Data
    {
        public readonly Dictionary<CardModel, int> amountsForPlayedCards = new();

        // 本能力召唤出来的青州兵（独立个体）：用于“回合开始时移除”以及“移除时不触发本能力”。
        public readonly HashSet<Creature> summonedSoldiers = new();

        // 正在被本能力清场的青州兵：这批死亡不算“生物死亡”，不给力量。
        public readonly HashSet<Creature> soldiersBeingRemoved = new();
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 可叠加：Counter 显示当前层数（= 已打出的“曹氏兵法”张数），每层提供相同的触发强度
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 出牌、死亡与回合开始钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 能力图标资源（Counter 层数会显示在图标角标上）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 悬停提示：“募集”说明（数量取当前层数 = 每打出一张牌募集的青州兵数）。
    // 能力描述里的“募集”是金色文本，解释由这条提示提供；文案见 Scripts/Cards/RecruitKeyword.cs。
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        RecruitKeyword.Recruit(Amount)
    ];

    // 供补丁类判定“这只青州兵是不是本能力召唤的独立个体”，命中则从名单里移除（只消费一次）。
    // 见 Scripts/Patches/CaoArtOfWarSoldierRemovalPatch.cs
    internal static bool TryConsumeSummonedSoldier(Creature ownerCreature, Creature soldier)
    {
        CaoArtOfWarPower? power = ownerCreature.GetPower<CaoArtOfWarPower>();
        bool consumed = power is not null && power.GetInternalData<Data>().summonedSoldiers.Remove(soldier);
        return WorthAllTheirLivesPower.TryConsumeSoldier(ownerCreature, soldier) || consumed;
    }

    // 供伤害改道补丁判定“这次伤害该不该由青州兵依次承担”：
    // 任一募集来源的青州兵里还有活着的（不要求曹氏兵法能力在场）。
    // 见 Scripts/Patches/CaoArtOfWarSoldierDamageCascadePatch.cs
    internal static bool HasLivingSummonedSoldier(Creature ownerCreature)
    {
        return GetLivingSoldiersInSummonOrder(ownerCreature).Count > 0;
    }

    // 承伤优先级（用户 2026-10-09 指定）：青州兵 → 玩家格挡 → 奥斯提 → 玩家。
    // 承伤顺序：PlayerCombatState.Pets 保持入场顺序，所以越早召唤的越先挨打（最旧 → 最新）。
    // 这里只取本能力召唤的青州兵（不含奥斯提）：奥斯提仍由原版 DieForYouPower 的改道接住，
    // 于是形成“青州兵先挡、奥斯提兜底”的层次（余量伤害交回原版时会被原版改道给活着的奥斯提）。
    internal static IReadOnlyList<Creature> GetLivingSoldiersInSummonOrder(Creature ownerCreature)
    {
        Player? owner = ownerCreature.Player;
        if (owner is null)
        {
            return System.Array.Empty<Creature>();
        }
        return owner.PlayerCombatState.Pets
            .Where(pet => pet.Monster is QingzhouSoldier && pet.IsAlive)
            .ToList();
    }

    // 打牌开始前：记录此刻的层数
    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        // 只统计本能力拥有者打出的牌（多人模式下过滤其他玩家）
        if (Owner is null || card is null || card.Owner?.Creature != Owner)
        {
            return Task.CompletedTask;
        }
        GetInternalData<Data>().amountsForPlayedCards.Add(card, Amount);
        return Task.CompletedTask;
    }

    // 每当你打出一张牌时：按记账的层数独立召唤青州兵（账本里没有这张牌则不触发，每张牌只结算一次）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel? card = cardPlay.Card;
        Player? player = card?.Owner;
        if (Owner is null || !Owner.IsAlive || card is null || player is null || player.Creature != Owner)
        {
            return;
        }
        if (!GetInternalData<Data>().amountsForPlayedCards.Remove(card, out int amount) || amount <= 0)
        {
            return;
        }

        if (GetLivingSoldiersInSummonOrder(Owner).Count >= MaxLivingSoldiers)
        {
            return;
        }

        Flash();
        for (int i = 0; i < amount; i++)
        {
            // 每次生成前重新检查，叠层或召唤钩子引发的嵌套募集也不能越过上限。
            if (GetLivingSoldiersInSummonOrder(Owner).Count >= MaxLivingSoldiers)
            {
                break;
            }
            Creature? soldier = await SummonIndependentSoldier(choiceContext, player);
            if (soldier is not null)
            {
                GetInternalData<Data>().summonedSoldiers.Add(soldier);
            }
        }
        // 站位不需要我们操心：青州兵不是 Osty，引擎会走“通用宠物排布”自动把它们在玩家身后错开
        // （NCombatRoom.AddCreature 的 :407015-407035 与 PositionPlayersAndPets 的 :406917-406922）。
    }

    // 每当任何生物死亡时：获得与层数相同的力量（本能力清场移除的青州兵除外）
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner is null || !Owner.IsAlive || Amount <= 0)
        {
            return;
        }
        // 回合开始时的那次清场不算“生物死亡”，不触发本能力
        if (GetInternalData<Data>().soldiersBeingRemoved.Remove(creature))
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
    }

    // 在你的回合开始时：移除本能力召唤的所有青州兵（此过程不触发本能力）
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner is null || player is null || player.Creature != Owner)
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.summonedSoldiers.Count == 0)
        {
            return;
        }

        List<Creature> soldiers = data.summonedSoldiers.Where(c => c.IsAlive).ToList();
        foreach (Creature soldier in soldiers)
        {
            data.soldiersBeingRemoved.Add(soldier);
        }
        foreach (Creature soldier in soldiers)
        {
            await CreatureCmd.Kill(soldier);
        }

        // 死亡被阻止（例如免疫）的个体下个回合再清；名单里已死的个体已被补丁消费掉
        data.soldiersBeingRemoved.Clear();
        data.summonedSoldiers.Clear();
        foreach (Creature soldier in soldiers)
        {
            if (soldier.IsAlive)
            {
                data.summonedSoldiers.Add(soldier);
            }
        }
    }

    /// <summary>
    /// 独立召唤 1 只 1 生命值的青州兵（不并进已有的任何召唤物）。
    /// 流程对齐原版 <c>OstyCmd.Summon</c> 的新建分支：AddPet → 挂“替你去死” → 设定生命值 → 结算召唤事件。
    /// 原版的 ModifySummonAmount 钩子在这里不适用：本次召唤的生命值是固定的 1，不随任何加成变化。
    /// </summary>
    internal static async Task<Creature?> SummonIndependentSoldier(PlayerChoiceContext choiceContext, Player player)
    {
        if (GetLivingSoldiersInSummonOrder(player.Creature).Count >= MaxLivingSoldiers)
        {
            return null;
        }
        Creature soldier = await PlayerCmd.AddPet<QingzhouSoldier>(player);
        // 青州兵的初始生命值本就是 1（MinInitialHp / MaxInitialHp = 1），这里显式设一遍以免上游改动
        await CreatureCmd.SetMaxHp(soldier, IndependentSoldierHp);
        await CreatureCmd.Heal(soldier, IndependentSoldierHp, false);
        // “替你去死”：让这只青州兵替你承伤（原版对新建的奥斯提做同样的事）
        await PowerCmd.Apply<DieForYouPower>(choiceContext, soldier, 1m, null, null);

        var combatState = player.Creature.CombatState;
        if (combatState is null)
        {
            return soldier;
        }
        CombatManager.Instance.History.Summoned(combatState, (int)IndependentSoldierHp, player);
        await Hook.AfterSummon(combatState, choiceContext, player, IndependentSoldierHp);
        return soldier;
    }
}
