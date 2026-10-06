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

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 12 点伤害（升级 16）
    // IncreaseBy 只用于卡面预览：瞄准敌人时显示“本次会永久增加多少伤害”（见 ProxyStrikeIncreaseVar）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar(InitialDamageVarName, 12m),
        new ProxyStrikeIncreaseVar()
    ];

    // 标签：视为“打击”，使依赖打击标签的遗物/卡牌可与之交互
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    // 已经从敌人意图“借”到的最高伤害值
    // （引擎的 DowngradeInternal 会用 CanonicalVars 重建 DynamicVars，
    //   把贴上去的伤害冲掉，所以用这个字段在降级后重新贴回去——与「利爪」Claw 同款写法）
    private int _adoptedIntentDamage;

    private int AdoptedIntentDamage
    {
        get => _adoptedIntentDamage;
        set
        {
            AssertMutable();
            _adoptedIntentDamage = value;
        }
    }

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
        decimal initialDamage = DynamicVars[InitialDamageVarName].BaseValue;
        int intentDamage = GetIntentDamageTowards(owner, target);
        if (intentDamage > initialDamage)
        {
            IncreaseBaseDamageBy(intentDamage - initialDamage);
        }
    }

    // 把这张牌的伤害初始值永久提高 delta：
    // Damage（实际结算用）与 InitialDamage（卡面显示的“初始值”）必须同步，二者始终相等
    private void IncreaseBaseDamageBy(decimal delta)
    {
        if (delta <= 0m)
        {
            return;
        }

        DynamicVar initialDamageVar = DynamicVars[InitialDamageVarName];
        initialDamageVar.BaseValue += delta;
        DynamicVars.Damage.BaseValue += delta;
        AdoptedIntentDamage = initialDamageVar.IntValue;
    }

    // 降级后：引擎已用 CanonicalVars 重建 DynamicVars，把累计增加的伤害贴回去
    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVar initialDamageVar = DynamicVars[InitialDamageVarName];
        if (AdoptedIntentDamage > initialDamageVar.BaseValue)
        {
            decimal delta = AdoptedIntentDamage - initialDamageVar.BaseValue;
            initialDamageVar.BaseValue += delta;
            DynamicVars.Damage.BaseValue += delta;
        }
    }

    // 升级后的效果逻辑
    protected override void OnUpgrade()
    {
        // 伤害从 12 提高到 16（Damage 与卡面显示的初始值一起提）
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars[InitialDamageVarName].UpgradeValueBy(4m);
    }

    /// <summary>
    /// 卡面显示的“本次会永久增加多少伤害”：目标意图伤害高于这张牌的伤害初始值时二者的差值，否则 0。
    /// 只用于预览（<see cref="ProxyStrikeIncreaseVar"/>），真正的永久增加走 <see cref="IncreaseBaseDamageBy"/>。
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
