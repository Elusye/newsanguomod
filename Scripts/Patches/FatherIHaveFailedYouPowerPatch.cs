using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;

using newsanguo.Scripts.Characters;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

// 每场战斗开始时给新三国角色挂上「孩儿不孝啊！」：受伤语音与该能力的 1 血语音都靠它发声。
//
// 接入点：Hook.BeforeCombatStart —— 战斗开始、生物全部加入后的统一钩子，此刻生命值已是本次战斗的初始值。
// 施加发生在钩子枚举结束之后（postfix），不会干扰正在进行的钩子遍历。
//
// 多人注意：本作是确定性同步，能力属于**同步状态**，所以每台机器必须把能力挂到同一批生物上。
// 早期版本只给本机玩家挂（LocalContext.GetMe），结果两台机器各自把能力挂到了自己的玩家身上，
// 第一场战斗第 1 回合开始的第一次校验就分歧、客户端被踢回主菜单（2026-09-20 的联机日志已复现）。
// 现在遍历 runState.Players 给“所有新三国角色”挂上：各机器遍历顺序与结果完全一致。
// “谁受伤时该在谁的机器上出声”交给能力内部按 LocalContext.IsMe 判断，见 FatherIHaveFailedYouPower。
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
public static class FatherIHaveFailedYouPowerPatch
{
    public static void Postfix(IRunState runState)
    {
        foreach (Player player in runState.Players)
        {
            if (player.Character is not NewsanguoCharacter)
            {
                continue;
            }
            // 施加能力不会触发任何玩家选择，用 ThrowingPlayerChoiceContext 即可
            // （本体 Sleight of Flesh Power 在同样场景下就是这么做的）
            _ = TaskHelper.RunSafely(PowerCmd.Apply<FatherIHaveFailedYouPower>(
                new ThrowingPlayerChoiceContext(),
                player.Creature,
                1,
                player.Creature,
                cardSource: null,
                silent: true));
        }
    }
}
