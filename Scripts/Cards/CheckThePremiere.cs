using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class CheckThePremiere : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public CheckThePremiere() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/check_the_premiere");

        // 从五张“0.99 版本后被削弱”的原始版本卡牌（不含燃料）中随机选出一张，
        // 使用战斗生成 RNG 保证多人同步（与 Splash 一致）
        CardModel[] templates =
        [
            combatState.CreateCard<OldForgottenRitual>(base.Owner),
            combatState.CreateCard<OldBorrowedTime>(base.Owner),
            combatState.CreateCard<OldDirge>(base.Owner),
            combatState.CreateCard<OldCompact>(base.Owner),
            combatState.CreateCard<OldExpectAFight>(base.Owner)
        ];
        // 旧版卡牌设了 CanBeGeneratedInCombat = false（避免进入无色牌随机生成池），
        // 不能走 CardFactory.GetDistinctForCombat（其内部 FilterForCombat 会把这些牌全部过滤掉），
        // 这里直接用战斗生成 RNG 从模板中随机取 1 张，保证多人同步。
        CardModel? selected = templates.TakeRandom(1, base.Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        // 升级后的“看看首播版”，获得的卡牌为升级版本
        if (IsUpgraded)
        {
            CardCmd.Upgrade(selected);
        }

        // 将获得的卡牌加入手牌
        var result = await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, base.Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(result, 1.2f);
    }
}
