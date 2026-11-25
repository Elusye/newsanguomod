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

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class CheckThePremiere : NewsanguoCardTemplate
{

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, .. base.CanonicalKeywords];

    public CheckThePremiere() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState combatState = base.CombatState!;

        // 播放出牌音效
        NewsanguoSfx.Play("event:/newsanguo/sfx/check_the_premiere");

        // 从现有旧版牌范围中随机提供三张不同候选，
        // 使用战斗生成 RNG 保证多人同步（与 Splash 一致）
        CardModel[] templates =
        [
            combatState.CreateCard<OldForgottenRitual>(base.Owner),
            combatState.CreateCard<OldBorrowedTime>(base.Owner),
            combatState.CreateCard<OldDirge>(base.Owner),
            combatState.CreateCard<OldCompact>(base.Owner),
            combatState.CreateCard<OldExpectAFight>(base.Owner),
            combatState.CreateCard<OldFuel>(base.Owner),
            combatState.CreateCard<OldMementoMori>(base.Owner),
            combatState.CreateCard<OldMythicForm>(base.Owner),
            combatState.CreateCard<OldTheHierophant>(base.Owner),
            combatState.CreateCard<OldTheEmperor>(base.Owner),
            combatState.CreateCard<OldTheTower>(base.Owner),
            combatState.CreateCard<OldTemperance>(base.Owner),
            combatState.CreateCard<OldTheSun>(base.Owner),
            combatState.CreateCard<OldTheHermit>(base.Owner),
            combatState.CreateCard<OldDeath>(base.Owner)
        ];
        // 旧版牌禁止通用随机生成，直接抽样以保留这些专属候选。
        List<CardModel> options = templates.TakeRandom(3, Owner.RunState.Rng.CombatCardGeneration).ToList();
        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, options, Owner, canSkip: false);
        if (selected is null)
            return;

        selected.SetToFreeThisTurn();

        // 将获得的卡牌加入手牌
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, base.Owner);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);

    protected override void AfterDowngraded() => AddKeyword(CardKeyword.Exhaust);
}
