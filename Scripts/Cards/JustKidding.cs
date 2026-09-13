using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class JustKidding : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public JustKidding() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/just_kidding");

        // 从手牌中选择任意一张牌（可放弃选择）
        var cardModel = (
            await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                player: Owner,
                filter: null,
                source: this
            )
        ).FirstOrDefault();

        if (cardModel != null)
        {
            // 放到抽牌堆顶部，且在该牌被打出之前其耗能变为 0
            await CardPileCmd.Add(cardModel, PileType.Draw, CardPilePosition.Top);
            cardModel.EnergyCost.SetUntilPlayed(0);
        }
    }

    // 升级：费用 1 → 0
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
