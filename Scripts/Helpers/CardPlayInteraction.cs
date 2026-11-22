using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace newsanguo.Scripts.Helpers;

/// <summary>筛除出牌逻辑会打开选牌或预见窗口的牌；自带消耗词条不属于选牌交互。</summary>
internal static class CardPlayInteraction
{
    private static readonly ConcurrentDictionary<Type, bool> SelectionCache = new();

    internal static bool RequiresSelection(CardModel card) =>
        SelectionCache.GetOrAdd(card.GetType(), Inspect);

    private static bool Inspect(Type cardType)
    {
        var onPlay = AccessTools.Method(cardType, "OnPlay");
        if (onPlay is null)
            return true;

        var pending = new Stack<MethodBase>();
        var seen = new HashSet<MethodBase>();
        pending.Push(onPlay);
        while (pending.TryPop(out var method))
        {
            if (!seen.Add(method))
                continue;
            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } asyncMethod)
            {
                var moveNext = AccessTools.Method(asyncMethod.StateMachineType, "MoveNext");
                if (moveNext is not null)
                    pending.Push(moveNext);
            }
            if (method.GetMethodBody() is null)
                continue;

            foreach (var instruction in PatchProcessor.GetOriginalInstructions(method))
            {
                if (instruction.operand is not MethodBase called || called.DeclaringType is not { } type)
                    continue;
                // 预见有原版与本 mod 两个命令入口，均需要打开窗口。
                if (type.Name is "CardSelectCmd" or "ScryCmd")
                    return true;
                // 包括继承的出牌实现、本地函数、异步状态机及同程序集的封装方法。
                // 不深入伤害、抽牌等引擎公共命令，避免把其他效果的窗口误算作本牌交互。
                if (type.Assembly == onPlay.DeclaringType!.Assembly &&
                    (BelongsToCard(type, cardType) ||
                     !typeof(CardModel).IsAssignableFrom(type) &&
                     type.Namespace == onPlay.DeclaringType.Namespace))
                    pending.Push(called);
            }
        }
        return false;
    }

    private static bool BelongsToCard(Type type, Type cardType)
    {
        while (type.DeclaringType is { } parent)
            type = parent;
        return typeof(CardModel).IsAssignableFrom(type) && type.IsAssignableFrom(cardType);
    }
}
