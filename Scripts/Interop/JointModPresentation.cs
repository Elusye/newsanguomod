using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace newsanguo.Scripts.Interop;

/// <summary>
/// 跨模组联动在卡面上的三种展示状态。
/// 玩法判定仍按卡牌/能力的实际拥有者处理，不使用这里的本地玩家判定。
/// </summary>
internal enum JointModPresentationMode
{
    None,
    HoverOnly,
    InlineKeywords
}

/// <summary>
/// 统一实现联动展示的 A/B/C 规则。展示状态以本地玩家为准，
/// 这样联机时不会把队友角色的联动关键词错误显示到本地玩家的卡面上。
/// </summary>
internal static class JointModPresentation
{
    public static JointModPresentationMode Resolve(
        bool isModReady,
        Func<CharacterModel?, bool> isJointModCharacter)
    {
        RunState? runState = RunManager.Instance.DebugOnlyGetState();
        if (runState is null)
        {
            return Resolve(isModReady, false, false, false, false);
        }

        bool partyContainsJointModCharacter = runState.Players
            .Any(player => isJointModCharacter(player.Character));
        bool localPlayerIsJointModCharacter =
            isJointModCharacter(LocalContext.GetMe(runState)?.Character);

        return Resolve(
            isModReady,
            true,
            CombatManager.Instance is { IsInProgress: true },
            partyContainsJointModCharacter,
            localPlayerIsJointModCharacter);
    }

    internal static JointModPresentationMode Resolve(
        bool isModReady,
        bool isInRun,
        bool isInCombat,
        bool partyContainsJointModCharacter,
        bool localPlayerIsJointModCharacter)
    {
        if (!isModReady)
        {
            return JointModPresentationMode.None;
        }

        if (!isInRun)
        {
            return JointModPresentationMode.HoverOnly;
        }

        if (!partyContainsJointModCharacter)
        {
            return JointModPresentationMode.None;
        }

        if (!isInCombat)
        {
            return JointModPresentationMode.HoverOnly;
        }

        return localPlayerIsJointModCharacter
            ? JointModPresentationMode.InlineKeywords
            : JointModPresentationMode.HoverOnly;
    }
}
