using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Events;

/// <summary>
/// 陈留大食堂（第一幕事件）：
/// 选项1：回复最大生命值的1/3（描述中用动态变量显示具体数值）；
/// 选项2：最大生命值+5；
/// 选项3：将一张原版诅咒「悔恨」加入牌组，并获得一件随机遗物。
/// </summary>
// 任意一幕均可遇到：第一幕可能是 Underdocks 或 Overgrowth，第二幕 Hive，第三幕 Glory
[RegisterActEvent(typeof(Underdocks))]
[RegisterActEvent(typeof(Overgrowth))]
[RegisterActEvent(typeof(Hive))]
[RegisterActEvent(typeof(Glory))]
public class chenliu_mess_hall : ModEventTemplate
{
    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: $"res://newsanguo/images/events/{GetType().Name}.png"
    );

    /// <summary>选项1的回复量 = 最大生命值 / 3（向下取整）。</summary>
    private int HealAmount => Owner == null ? 0 : (int)(Owner.Creature.MaxHp / 3m);

    // 让选项1的描述能显示具体回复数值：{HealAmount}
    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar("HealAmount", 0m)];

    public override void CalculateVars()
    {
        DynamicVars["HealAmount"].BaseValue = HealAmount;
    }

    // 进入事件时播放音效（EventRoom.Enter 只会对本地事件调用此方法，多人下不会重复播放）
    // 对应 FMOD 事件 event:/newsanguo/sfx/chenliu_mess_hall，需在 FMOD 中补齐后重新导出 bank
    public override Task AfterEventStarted()
    {
        SfxCmd.Play("event:/newsanguo/sfx/chenliu_mess_hall");
        return Task.CompletedTask;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, HealThird, InitialOptionKey("HEAL_THIRD")),
            new EventOption(this, GainMaxHp, InitialOptionKey("GAIN_MAX_HP")),
            new EventOption(this, RegretAndRelic, InitialOptionKey("REGRET_RELIC"))
        ];
    }

    private async Task HealThird()
    {
        // 对应 FMOD 事件 event:/newsanguo/sfx/chenliu_mess_hall_heal
        SfxCmd.Play("event:/newsanguo/sfx/chenliu_mess_hall_heal");
        await CreatureCmd.Heal(Owner!.Creature, HealAmount);
        SetEventFinished(PageDescription("HEAL_THIRD"));
    }

    private async Task GainMaxHp()
    {
        // 对应 FMOD 事件 event:/newsanguo/sfx/chenliu_mess_hall_max_hp
        SfxCmd.Play("event:/newsanguo/sfx/chenliu_mess_hall_max_hp");
        await CreatureCmd.GainMaxHp(Owner!.Creature, 5m);
        SetEventFinished(PageDescription("GAIN_MAX_HP"));
    }

    private async Task RegretAndRelic()
    {
        // 对应 FMOD 事件 event:/newsanguo/sfx/chenliu_mess_hall_relic
        SfxCmd.Play("event:/newsanguo/sfx/chenliu_mess_hall_relic");
        // 将一张原版诅咒「悔恨」加入牌组
        CardModel regret = Owner!.RunState.CreateCard<Regret>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(regret, PileType.Deck));
        // 获得一件随机遗物：按标准稀有度概率抽取，本局不会重复出现同一件
        RelicModel relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
        await RelicCmd.Obtain(relic, Owner);
        SetEventFinished(PageDescription("REGRET_RELIC"));
    }
}
