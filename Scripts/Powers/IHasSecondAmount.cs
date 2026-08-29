using System;
using System.Runtime.CompilerServices;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// 能力“双数字”显示接口（复刻自 BaseLib 的 IHasSecondAmount 机制）。
/// 引擎的能力图标默认在右下角显示 Amount（配合 PowerStackType.Counter）。
/// 实现本接口后，配合 SecondAmountLabelPatch，能力图标右上角会额外显示
/// <see cref="GetSecondAmount"/> 返回的文本。
/// 右上角标签由补丁复制引擎原版“%AmountLabel”节点并用原版 SetTextAutoSize 渲染，
/// 复用引擎原版主题字号与缩放逻辑，因此不会出现字体放大的问题。
/// 数值变化时调用 <see cref="SecondAmountPowerExtensions.InvokeSecondAmountChanged"/> 刷新图标。
/// </summary>
public interface IHasSecondAmount
{
    /// <summary>
    /// 返回能力图标右上角显示的第二数字文本（返回空字符串时隐藏右上角标签）。
    /// </summary>
    string GetSecondAmount();
}

/// <summary>
/// 将能力实例与其 NPower 节点的刷新动作关联的注册表。
/// 由 <see cref="SecondAmountLabelPatch"/> 在节点订阅/退订模型事件时维护。
/// </summary>
internal static class SecondAmountRegistry
{
    private static readonly ConditionalWeakTable<IHasSecondAmount, Action> RefreshActions = new();

    public static void Register(IHasSecondAmount power, Action refresh)
    {
        RefreshActions.AddOrUpdate(power, refresh);
    }

    public static void Unregister(IHasSecondAmount power)
    {
        RefreshActions.Remove(power);
    }

    public static void Refresh(IHasSecondAmount power)
    {
        if (RefreshActions.TryGetValue(power, out Action? refresh))
        {
            refresh();
        }
    }
}

/// <summary>
/// 供能力在第二数字变化时调用，触发其 NPower 节点刷新右上角标签。
/// </summary>
public static class SecondAmountPowerExtensions
{
    public static void InvokeSecondAmountChanged(this IHasSecondAmount power)
    {
        SecondAmountRegistry.Refresh(power);
    }
}
