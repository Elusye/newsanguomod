using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace newsanguo.Scripts.Patches;

/// <summary>联机开局随机角色统一排除蜀汉，本地选人界面仍由测试模式控制。</summary>
[HarmonyPatch(typeof(StartRunLobby), "BeginRunLocally")]
public static class ShuHanMultiplayerRandomSelectionPatch
{
    // BeginRunLocally 是同步方法；用作用域与 RitsuLib 的随机候选过滤同时生效。
    [ThreadStatic]
    private static bool _excludeShuHan;
    public static bool ExcludeShuHan => _excludeShuHan;

    public static void Prefix(StartRunLobby __instance, out bool __state)
    {
        __state = _excludeShuHan;
        _excludeShuHan = __instance.NetService.Type.IsMultiplayer();
    }

    public static void Finalizer(bool __state)
    {
        // 包括异常退出在内，都恢复原作用域，避免影响后续单人随机选人。
        _excludeShuHan = __state;
    }
}
