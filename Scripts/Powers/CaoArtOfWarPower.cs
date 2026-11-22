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
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Monsters;
namespace newsanguo.Scripts.Powers;

/// <summary>骄兵必败：每层提供回合结束时募集的一名青州兵。</summary>
[RegisterPower]
public class CaoArtOfWarPower : ModPowerTemplate
{
    // 独立召唤的青州兵生命值固定为 1（需求：以 1 生命值独立召唤）
    private const decimal IndependentSoldierHp = 1m;

    // 每位玩家同时存活的募集青州兵最多 15 名，阵亡后可再次募集补足。
    private const int MaxLivingSoldiers = 15;

    private class Data
    {
        public readonly HashSet<Creature> summonedSoldiers = new();
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 层数表示每回合结束时募集的数量。
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
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

    // 悬停提示显示当前每回合的募集数量。
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

    // Pets preserves summon order. Only living soldiers belonging to this player intercept damage.
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

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (!result.WasTargetKilled || dealer is not { IsAlive: true } || dealer.Side == Owner.Side ||
            result.Receiver is not { Monster: QingzhouSoldier } soldier || soldier.PetOwner?.Creature != Owner)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(context, dealer, -1m, Owner, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side,
        IEnumerable<Creature> participants)
    {
        Data data = GetInternalData<Data>();
        if (side == CombatSide.Enemy)
        {
            // 到期直接离场，不播放死亡动画，也不触发死亡奖励。
            foreach (Creature soldier in data.summonedSoldiers.Where(c => c.IsAlive && c.CombatState is not null).ToList())
                await DespawnSoldier(soldier);
            data.summonedSoldiers.Clear();
            return;
        }

        if (side != Owner.Side || !Owner.IsAlive || Owner.Player is not { } player || !participants.Contains(Owner) ||
            CombatManager.Instance.IsOverOrEnding || Amount <= 0 ||
            GetLivingSoldiersInSummonOrder(Owner).Count >= MaxLivingSoldiers)
            return;

        Flash();
        int count = Amount;
        for (int i = 0; i < count; i++)
        {
            if (!Owner.IsAlive || CombatManager.Instance.IsOverOrEnding) break;
            var soldier = await SummonIndependentSoldier(context, player);
            if (soldier is null) break;
            data.summonedSoldiers.Add(soldier);
        }
    }

    // Escape 不触发死亡事件，须同时清理原版只在 OnPetDied 中维护的宠物列表。
    internal static async Task DespawnSoldier(Creature soldier)
    {
        if (soldier.PetOwner?.PlayerCombatState is { } combat)
        {
            if (combat.Pets is IList<Creature> pets)
                pets.Remove(soldier);
            var handler = typeof(PlayerCombatState).GetMethod("OnPetDied",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            soldier.Died -= (System.Action<Creature>)handler.CreateDelegate(typeof(System.Action<Creature>), combat);
        }
        await CreatureCmd.Escape(soldier);
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
