using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(RetiredCardPool))]
public class ProxyStrike : NewsanguoCardTemplate
{
    public override bool CanBeGeneratedByModifiers => Pool is not RetiredCardPool;
    public override bool CanBeGeneratedInCombat => Pool is not RetiredCardPool;

    private const int BaseDamage = 12;
    private const int UpgradeDamageBonus = 4;
    private int _combatDamageBonus;
    private int _permanentDamageBonus;

    // 保留旧存档字段，战斗伤害只使用本场累计值。
    [SavedProperty]
    public int IncreasedDamage
    {
        get => _permanentDamageBonus;
        set
        {
            AssertMutable();
            _permanentDamageBonus = value;
        }
    }

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(BaseDamage, ValueProp.Move),
        new ProxyStrikeIncreaseVar()
    ];

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

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

        // 先读取被攻击前的意图，成长只影响随后的命中。
        int intentDamage = GetIntentDamageTowards(owner, target);
        decimal dealtValue = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(dealtValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        _combatDamageBonus += intentDamage;
        DynamicVars.Damage.BaseValue += intentDamage;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue = BaseDamage + _combatDamageBonus;
    }

    internal static decimal ComputeIntentIncrease(CardModel card, Creature? target) =>
        target is not null && card.IsMutable && card.Owner is { } owner
            ? Math.Max(0m, GetIntentDamageTowards(owner, target))
            : 0m;

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
