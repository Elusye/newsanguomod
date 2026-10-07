using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class ProxyStrike : NewsanguoCardTemplate
{
    // 卡面文案里的名字：这张牌的伤害初始值（不带力量修正的“原值”）
    // DamageVar 的 Damage 在手牌里会显示成力量等修正后的数值，
    // 所以比较基准（“大于12/16”）单独用 InitialDamage 这个不变修正的镜像变量。
    private const string InitialDamageVarName = "InitialDamage";

    // 基础伤害；升级加 4（12 → 16）
    private const int BaseDamage = 12;
    private const int UpgradeDamageBonus = 4;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 12 点伤害（升级 16）
    // 永久成长与升级都由 SyncDamageVars() 同步到这两个变量上。
    // IncreaseBy 只用于卡面预览：瞄准敌人时显示“本次会永久增加多少伤害”（见 ProxyStrikeIncreaseVar）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(BaseDamage, ValueProp.Move),
        new DynamicVar(InitialDamageVarName, (decimal)BaseDamage),
        new ProxyStrikeIncreaseVar()
    ];

    // 标签：视为“打击”，使依赖打击标签的遗物/卡牌可与之交互
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 累计“从敌人意图借来的”永久伤害成长。
    //
    // 2026-10-07 修：这一段以前是个普通字段（_adoptedIntentDamage），只活在战斗内的那张副本上，
    // 战斗一结束就随副本一起丢掉，所以“永久增加伤害”跨不了战斗。
    // 现在参照原版「基因算法」GeneticAlgorithm.cs:140745-140825（本仓库同款：
    // BetterThanYilingFlames.cs:32-80）：
    //   · 用 [SavedProperty] 让成长值跟随牌库本体（DeckVersion）存档；
    //   · 牌库本体是每场战斗开打时被克隆进抽牌堆的（Player.PopulateCombatState，sts2.decompiled.cs:177220-177229），
    //     且克隆会连字段带 DynamicVars 一起复制，所以下一场战斗天然继承；
    //   · 属性 setter 里同步 DynamicVars，读档（CardModel.FromSerializable，:75146-75170）回填属性时卡面数值立刻跟上。
    private int _increasedDamage;

    // 累计的永久成长值
    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
            SyncDamageVars();
        }
    }

    // 这张牌现在的伤害值 = 基础 12 + 永久成长 + 升级加成
    // （升级加成按 CurrentUpgradeLevel 算，读档时引擎先回填属性、再逐级跑 UpgradeInternal，
    //   两种顺序都能得到同一个结果）
    public int CurrentDamage => BaseDamage + IncreasedDamage + CurrentUpgradeLevel * UpgradeDamageBonus;

    public ProxyStrike() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        Creature target = cardPlay.Target;
        Player? owner = base.Owner;
        if (owner is null)
        {
            return;
        }

        NewsanguoSfx.Play("event:/newsanguo/sfx/proxy_strike");

        // 先按“当前的”伤害初始值结算本次伤害
        // （这里先把数值取出来，后面改高初始值不会影响本次这一击）
        decimal dealtValue = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(dealtValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 若目标本回合意图对这张牌的主人造成的伤害高于这张牌的伤害初始值，
        // 就把这张牌的伤害永久提高二者的差值（对以后的打出生效）
        int initialDamage = DynamicVars[InitialDamageVarName].IntValue;
        int intentDamage = GetIntentDamageTowards(owner, target);
        int gainedDamage = intentDamage - initialDamage;
        if (gainedDamage <= 0)
        {
            return;
        }

        // 战斗内这张副本：立刻变强
        AddPermanentDamage(gainedDamage);

        // 牌库本体（DeckVersion）：成长记在它身上，跨战斗保留。
        // 复制品/衍生牌的 DeckVersion 会被引擎清空（CardModel.AfterCloned，:74128），
        // 它们打出的成长只在本场战斗内有效——与原版「基因算法」一致。
        if (base.DeckVersion is ProxyStrike deckCopy && !ReferenceEquals(deckCopy, this))
        {
            deckCopy.AddPermanentDamage(gainedDamage);
        }
    }

    // 永久提高伤害成长值（DamageVar / InitialDamage 由 setter 里的 SyncDamageVars 同步）
    private void AddPermanentDamage(int delta)
    {
        if (delta <= 0)
        {
            return;
        }

        IncreasedDamage += delta;
    }

    // 把 Damage（实际结算）与 InitialDamage（卡面显示的“初始值”）同步成同一个数：
    // 基础伤害 + 永久成长 + 升级加成
    private void SyncDamageVars()
    {
        decimal value = CurrentDamage;
        DynamicVars.Damage.BaseValue = value;
        DynamicVars[InitialDamageVarName].BaseValue = value;
    }

    // 升级后的效果逻辑：伤害 12 → 16（与永久成长叠加，走同一套同步）
    protected override void OnUpgrade()
    {
        SyncDamageVars();
    }

    // 降级后：引擎已用 CanonicalVars 重建 DynamicVars，这里按“基础 + 永久成长”贴回去
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        SyncDamageVars();
    }

    // 卡面附加参数：告诉文案“本牌是不是复制品”。
    // 复制品的 DeckVersion 会被引擎清空（CardModel.AfterCloned，sts2.decompiled.cs:74119-74140），
    // 用它打出只会让本场这张副本变强，不会写回牌库本体——卡面上要说清楚。
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        AddIsCloneDescriptionArg(description);
    }

    /// <summary>
    /// 卡面显示的“本次会永久增加多少伤害”：目标意图伤害高于这张牌的伤害初始值时二者的差值，否则 0。
    /// 只用于预览（<see cref="ProxyStrikeIncreaseVar"/>），真正的永久增加走 <see cref="AddPermanentDamage"/>。
    /// </summary>
    internal static decimal ComputeIntentIncrease(CardModel card, Creature? target)
    {
        // 没有目标就不显示（也避免在不可变模型上读 Owner）
        if (target is null || card.Owner is not { } owner)
        {
            return 0m;
        }

        if (!card.DynamicVars.TryGetValue(InitialDamageVarName, out DynamicVar? initialDamageVar))
        {
            return 0m;
        }

        return Math.Max(0m, GetIntentDamageTowards(owner, target) - initialDamageVar.BaseValue);
    }

    /// <summary>
    /// 目标本回合所有攻击意图对 owner（这张牌的主人）造成的伤害合计。
    /// 口径与意图图标一致（含力量、易伤等伤害修正）。
    /// 原版 AttackIntent.GetSingleDamage 内部用 LocalContext.GetMe 取“本地玩家”当受击者，
    /// 多人模式下各客户端会算出不同数值（会破坏联机确定性），
    /// 因此这里显式传入受击者 owner.Creature，让所有客户端得到同一结果。
    /// </summary>
    private static int GetIntentDamageTowards(Player owner, Creature? target)
    {
        if (target?.Monster is not { } monster)
        {
            return 0;
        }

        Creature self = owner.Creature;
        int total = 0;

        foreach (AbstractIntent intent in monster.NextMove.Intents)
        {
            if (intent is not AttackIntent attack)
            {
                continue;
            }

            // 原始值取自招式定义（如盛碗虫（石）头槌 = 25）
            decimal raw = attack.DamageCalc?.Invoke() ?? 0m;

            // 按“受击者是 this 的主人”跑一次伤害修正，得到界面上显示的那个数
            decimal modified = Hook.ModifyDamage(
                owner.RunState,
                self.CombatState,
                self,
                target,
                raw,
                ValueProp.Move,
                null,
                null,
                ModifyDamageHookType.All,
                CardPreviewMode.None,
                out _);

            // 原版 GetSingleDamage 就是 Math.Max(0, (int)num)；多段意图再乘段数
            total += Math.Max(0, (int)modified) * Math.Max(1, attack.Repeats);
        }

        return total;
    }
}
