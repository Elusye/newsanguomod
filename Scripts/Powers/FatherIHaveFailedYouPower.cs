using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

// 孩儿不孝啊！（Father, I Have Failed You!）
//
// 角色常驻的表现型能力，本身不改动任何数值：受伤时喊一声（图标同时闪一下），
// 若以 1 点生命进入战斗则喊那句特殊的。
// 每场战斗开始时由 FatherIHaveFailedYouPowerPatch 施加，发声对应
// audios/player_hurt.mp3 与 audios/enter_combat_one_hp.mp3。
[RegisterPower]
public class FatherIHaveFailedYouPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;
    public override bool ShouldReceiveCombatHooks => true;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
    );

    // 每次真正掉血时喊一声：与本 mod 原来的做法一致（被完全格挡、掉血为 0 时不发声）。
    // 多人下这个能力会挂在所有新三国角色身上（见 FatherIHaveFailedYouPowerPatch），
    // 但只有“本机玩家的角色”受伤才在本机出声/闪光——队友受伤不该在你的机器上响。
    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || result.UnblockedDamage <= 0 || !LocalContext.IsMe(Owner))
        {
            return Task.CompletedTask;
        }
        Flash();
        NewsanguoSfx.Play("event:/newsanguo/sfx/player_hurt");
        return Task.CompletedTask;
    }

    // 首次获得（即每场战斗开始时由 FatherIHaveFailedYouPowerPatch 施加）：
    // 此刻生命值已是本次战斗的初始值，以 1 点生命进入战斗时喊那句特殊的。
    // 注意：只有 1 血这一种情况会发声，普通进入战斗不播语音；同样只在本机玩家的角色上发声。
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        if (Owner.CurrentHp == 1 && LocalContext.IsMe(Owner))
        {
            NewsanguoSfx.Play("event:/newsanguo/sfx/enter_combat_one_hp");
        }
    }
}
