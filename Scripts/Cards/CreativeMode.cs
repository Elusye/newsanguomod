using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts;

/// <summary>
/// 「创造模式」（Creative Mode）：蜀汉池的先古能力牌，4 费（升级后 3 费）。
/// 打出后挂上 <see cref="CreativeModePower" />，战斗结束时由该能力弹出选牌界面，
/// 从游戏里的全部卡牌（本牌自身除外）中自选一张加入牌组。
/// 2026-10-07：打出后不再从牌组里移除自身（一度做成"一次性牌"，已按要求撤销）。
/// 显示名/描述在 localization/*/cards.json 的 NEWSANGUO_CARD_CREATIVE_MODE.* 里；
/// 卡图路径按类名取 res://newsanguo/images/cards/CreativeMode.png。
/// </summary>
// 注册卡牌到蜀汉卡池（只在本池注册，曹魏拿不到）
[RegisterCard(typeof(ShuHanCardPool))]
public class CreativeMode : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public CreativeMode() : base(4, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/creative_mode");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 挂上「创造模式」能力：战斗结束时由它弹出“从所有牌中选一张加入牌组”的界面
        await PowerCmd.Apply<CreativeModePower>(
            choiceContext,
            base.Owner.Creature,
            1,
            base.Owner.Creature,
            this,
            silent: false);
    }

    // 升级后的效果逻辑：费用 4 → 3
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    // 这里故意不覆写 AfterDowngraded()：
    // CardModel.DowngradeInternal()（sts2.decompiled.cs:75049-75062）在调用 AfterDowngraded() 之前
    // 已经执行 EnergyCost.ResetForDowngrade()（:75055，内部 _base = Canonical），费用会自动回到 4。
    // 再补一句 EnergyCost.UpgradeBy(1) 反而会把 4 顶成 5 费（UpgradeBy 是相对当前 _base 加，见 :182038-182059）。
}
