using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 「君王形态」（Sovereign Form，蜀汉专属）：3 费（升级后仍是 3 费）稀有能力牌。
/// 施加 <see cref="SovereignFormPower"/>：你每获得 4 点酒力，获得 2 点天意之力；
/// 打出时另外将一张「参见汉中王！」（<see cref="HailKingOfHanzhong"/>）加入你的手牌。
///
/// 2026-10-07：本牌原名「真正的君王」（The True King），按要求改名为「君王形态」/ Sovereign Form，
/// 类名 TheTrueKing → SovereignForm、能力 TheTrueKingPower → SovereignFormPower、
/// 卡图/音效文件名与本地化 key 同步改名（旧 key NEWSANGUO_CARD_THE_TRUE_KING 已弃用）。
///
/// 卡图 res://newsanguo/images/cards/SovereignForm.png；音效 event:/newsanguo/sfx/sovereign_form。
/// 卡面文案在 localization/*/cards.json 的 NEWSANGUO_CARD_SOVEREIGN_FORM.*，
/// 能力文案在 localization/*/powers.json 的 NEWSANGUO_POWER_SOVEREIGN_FORM_POWER.*。
/// </summary>
[RegisterCard(typeof(ShuHanCardPool))]
public class SovereignForm : NewsanguoCardTemplate
{
    // 卡图资源
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://newsanguo/images/cards/{GetType().Name}.png"
    );

    // 卡牌基础数值：每次触发获得的天意之力 2（= 施加给能力的层数）、触发所需的酒力 4
    // （阈值直接引用能力侧常量，改数值只需改 SovereignFormPower 一处）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new HeavensForceVar(2m),
        new IntVar("WineThreshold", SovereignFormPower.WinePerTrigger)
    ];

    // 悬停提示：酒力、被加入手牌的「参见汉中王！」，以及天意之力资源说明
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<DrunkenMightPower>(),
        HoverTipFactory.FromCard<HailKingOfHanzhong>(),
        HeavensForce.HoverTip()
    ];

    // 属于“天意”体系（涉及天意之力）
    public override bool IsHeavensCard => true;

    // 3 费、稀有、能力牌、目标为自己（升级不降费，改加天意之力，见 OnUpgrade）
    public SovereignForm() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    // 打出时的效果逻辑：挂上能力，再把一张「参见汉中王！」塞进手牌
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放出牌音效（音频文件：newsanguo/audios/sovereign_form.mp3|wav|ogg）
        NewsanguoSfx.Play("event:/newsanguo/sfx/sovereign_form");

        // 播放角色施法动画
        await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);

        // 施加能力：层数 = 每次触发放给的天意之力（2 点）
        await PowerCmd.Apply<SovereignFormPower>(
            choiceContext,
            base.Owner.Creature,
            DynamicVars["HeavensForcePower"].IntValue,
            base.Owner.Creature,
            this,
            silent: false);

        // 将一张「参见汉中王！」加入手牌
        await HailKingOfHanzhong.CreateInHand(base.Owner, CombatState!);
    }

    // 升级后的效果逻辑：每次触发放给的天意之力 2 → 3
    // （费用保持 3 费不变；层数同时是施加给能力的 Amount，所以升级后每档变成 3 点）
    protected override void OnUpgrade()
    {
        DynamicVars["HeavensForcePower"].UpgradeValueBy(1);
    }
}
