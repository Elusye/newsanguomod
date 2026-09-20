using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

// 每场战斗开始时给本机的新三国角色挂上「孩儿不孝啊！」：受伤语音与该能力的 1 血语音都靠它发声。
//
// 接入点：Hook.BeforeCombatStart —— 战斗开始、生物全部加入后的统一钩子，此刻生命值已是本次战斗的初始值。
// 只处理本机玩家自己的角色：多人下各客户端各自照顾自己的角色，避免给远端玩家的角色重复施加能力。
// 施加发生在钩子枚举结束之后（postfix），不会干扰正在进行的钩子遍历。
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
public static class FatherIHaveFailedYouPowerPatch
{
    public static void Postfix(IRunState runState)
    {
        Player? me = LocalContext.GetMe(runState);
        if (me?.Character is not NewsanguoCharacter)
        {
            return;
        }
        // 施加能力不会触发任何玩家选择，用 ThrowingPlayerChoiceContext 即可
        // （本体 Sleight of Flesh Power 在同样场景下就是这么做的）
        _ = TaskHelper.RunSafely(PowerCmd.Apply<FatherIHaveFailedYouPower>(
            new ThrowingPlayerChoiceContext(),
            me.Creature,
            1,
            me.Creature,
            cardSource: null,
            silent: true));
    }
}
