using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Cards;
using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Helpers;

namespace newsanguo.Scripts;

// 注册卡牌到新三国专属卡池
[RegisterCard(typeof(NewsanguoCardPool))]
public class tweak : NewsanguoCardTemplate
{
    // 基础耗能：1
    private const int energyCost = 1;
    // 卡牌类型：攻击
    private const CardType type = CardType.Attack;
    // 卡牌稀有度：普通
    private const CardRarity rarity = CardRarity.Common;
    // 目标类型：任意敌人
    private const TargetType targetType = TargetType.AnyEnemy;
    // 是否在卡牌图鉴中显示
    private const bool shouldShowInCardLibrary = true;

    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：造成 6 点伤害
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move)
    ];

    public tweak() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    // 打出时的效果逻辑
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? owner = base.Owner;
        Creature? target = cardPlay.Target;
        if (owner is null || target is null)
        {
            return;
        }

        // 播放出牌音效
        SfxCmd.Play("event:/newsanguo/sfx/tweak");

        // 播放角色攻击动画
        await CreatureCmd.TriggerAnim(owner.Creature, "Attack", owner.Character.CastAnimDelay);

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .Execute(choiceContext);

        // 战斗中的卡牌本就是牌组卡牌的复制品，为战斗中的卡牌附魔不会影响牌组，
        // 因此直接为手牌添加随机附魔即可，无需复制再消耗原牌。
        if (IsUpgraded)
        {
            // 升级：为所有未附魔的手牌添加随机附魔
            foreach (CardModel hand in PileType.Hand.GetPile(owner).Cards.Where(c => c.Enchantment is null).ToArray())
            {
                EnchantHelper.ApplyRandomEnchant(hand, owner);
            }
        }
        else
        {
            // 选择一张未附魔的手牌，直接添加随机附魔
            CardModel? selected = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1),
                context: choiceContext,
                player: owner,
                filter: card => card.Enchantment is null,
                source: this)).FirstOrDefault();
            if (selected is null)
            {
                return;
            }
            EnchantHelper.ApplyRandomEnchant(selected, owner);
        }
    }

    // 升级后的效果逻辑（升级效果由 OnPlay 中的 IsUpgraded 分支实现）
    protected override void OnUpgrade()
    {
    }
}
