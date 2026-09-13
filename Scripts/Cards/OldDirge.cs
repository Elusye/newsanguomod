using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 挽歌（旧）：旧版本的挽歌，召唤 3X 次并将 X 张灵魂加入抽牌堆（升级后召唤 4X 次并加入灵魂+）
// 注册卡牌到衍生卡池
[RegisterCard(typeof(TokenCardPool))]
public class OldDirge : NewsanguoCardTemplate
{
    public override CardPoolModel VisualCardPool => ModelDb.CardPool<NecrobinderCardPool>();

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 每次召唤的奥斯蒂数量（升级后 4）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new SummonVar(3)
    ];

    // 悬停提示：展示“灵魂”卡牌的说明（升级后为灵魂+）
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<Soul>(IsUpgraded)
    ];

    // X 费牌（同原版旋风斩/天际钻头）：打出时自动花费全部剩余能量
    protected override bool HasEnergyCostX => true;

    public OldDirge() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑（参考原版挽歌 Dirge）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // X = 本回合为打出此牌花费的能量
        int x = ResolveEnergyXValue();

        // 召唤 3X 次（每次召唤 DynamicVars.Summon 只奥斯蒂）
        for (int i = 0; i < x; i++)
        {
            await OstyCmd.Summon(choiceContext, base.Owner, DynamicVars.Summon.BaseValue, this);
        }

        // 将 X 张灵魂加入抽牌堆（升级后为灵魂+）
        List<Soul> souls = Soul.Create(base.Owner, x, combatState).ToList();
        if (IsUpgraded)
        {
            foreach (Soul soul in souls)
            {
                CardCmd.Upgrade(soul);
            }
        }

        var result = await CardPileCmd.AddGeneratedCardsToCombat(souls, PileType.Draw, base.Owner, CardPilePosition.Bottom);
        CardCmd.PreviewCardPileAdd(result, 1.2f);
    }

    // 升级：每次召唤的奥斯蒂数量 3 → 4
    protected override void OnUpgrade()
    {
        DynamicVars.Summon.UpgradeValueBy(1);
    }
}
