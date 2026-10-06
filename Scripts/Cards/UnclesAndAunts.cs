using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class UnclesAndAunts : NewsanguoCardTemplate
{
    // X 费卡（参考原版“天际钻头”HeavenlyDrill）
    protected override bool HasEnergyCostX => true;

    // 鼠标悬停时自动显示格挡提示（CardModel.HoverTips 依据此属性添加 StaticHoverTip.Block）
    public override bool GainsBlock => true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：
    // 每次获得 3 点格挡（2026-10-05 起不再受酒力加成，只吃力量/敏捷），
    // 每次造成 4 点伤害（伤害侧的酒力加成由 DrunkenMightPower.ModifyDamageAdditive 自动生效）。
    // 力量与敏捷同时作用于本牌的伤害与格挡：引擎会各自正常结算一次
    // （伤害侧 StrengthPower.ModifyDamageAdditive、格挡侧 DexterityPower.ModifyBlockAdditive），
    // 交叉的那一半在 OnPlay 里手动补上：伤害额外 + 敏捷、格挡额外 + 力量。
    // 次数 = X，升级后 X+1。
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new BlockVar(3m, ValueProp.Move),
        new DamageVar(4m, ValueProp.Move)
    ];

    // 无自带关键词：不再有“保留”。此处不重写 CanonicalKeywords，
    // 以免覆盖模板附加的模组关键词（NewsanguoCardTemplate 的合并逻辑只在基类里，见其文档注释）。

    // 悬停提示：卡面文案提到了力量与敏捷，只给这两项
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public UnclesAndAunts() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    // 重放 1：打出后引擎会把整套 OnPlay 再执行一遍。原版实现见 CardModel.Play：
    //   int playCount = await GeneratePlayCount(...);  // = GetEnchantedReplayCount() + 1
    //   for (int i = 0; i < playCount; i++) { ... await OnPlay(...); }
    // 而 GetEnchantedReplayCount() = Enchantment?.EnchantPlayCount(BaseReplayCount) ?? BaseReplayCount。
    //
    // 注意 BaseReplayCount 的 setter 会 AssertMutable()，所以不能写进构造函数：
    // 构造函数跑的是"规范模型"（不可变），写它会抛 CanonicalModelException。
    // 引擎造可变副本的方式是 MutableClone() → MemberwiseClone() → DeepCloneFields()/AfterCloned()，
    // 所以在这里补上 1。用 < 1 判断，既避免反复克隆把重放次数越叠越高，也保留其他效果给的重放加成。
    // 悬停提示无需手动添加：CardModel.HoverTips 在本牌重放次数 > 0 时会自动附上"重放"提示。
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();

        if (BaseReplayCount < 1)
        {
            BaseReplayCount = 1;
        }
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/uncles_and_aunts");

        // 结算 X 的最终数值，升级后次数 +1（X 次 → X+1 次）
        var x = ResolveEnergyXValue();
        var repeat = x + (IsUpgraded ? 1 : 0);

        // 格挡数值只算一次：本张牌结算期间力量不会变化，因此 X 轮里每轮获得的格挡都相同。
        // 注意必须走“数值 + ValueProp”这个重载：BlockVar 重载只取 BaseValue，会漏掉下面手动加的力量。
        // 交叉加成：格挡额外 + 力量（敏捷由 CreatureCmd.GainBlock 内部的 ModifyBlockAdditive 正常结算）。
        // 临时力量/敏捷（TemporaryStrengthPower/TemporaryDexterityPower）内部施加的就是
        // StrengthPower/DexterityPower，所以 GetPowerAmount 读到的是玩家实际拥有的完整数值（可为负）。
        decimal blockAmount = DynamicVars.Block.BaseValue
            + Owner.Creature.GetPowerAmount<StrengthPower>();
        ValueProp blockProps = DynamicVars.Block.Props;

        // 交叉加成：伤害额外 + 敏捷（力量由攻击结算管线内的 ModifyDamageAdditive 正常结算）。
        decimal damageAmount = DynamicVars.Damage.BaseValue
            + Owner.Creature.GetPowerAmount<DexterityPower>();

        // 每轮先获得格挡再造成伤害，重复“次数”轮。
        // 不再返还能量（原先的“找零 1 点能量”已移除）。
        for (var i = 0; i < repeat; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, blockProps, cardPlay);
            await DamageCmd.Attack(damageAmount)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }
}
