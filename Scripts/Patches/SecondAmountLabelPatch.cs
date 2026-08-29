using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.addons.mega_text;

using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

// 复刻 BaseLib（Alchyr.Sts2.BaseLib）的 TwoAmountPowers 补丁机制：
// 让实现了 IHasSecondAmount 的能力图标在右上角额外显示一个数字（右下角仍显示 Amount）。
//
// 与 RitsuLib 的 ExtraIconAmountLabelSpec 角标方案不同，本方案不创建带自定义字号的角标节点，
// 而是直接复制引擎原版能力图标里的 "%AmountLabel"（MegaLabel）节点，用引擎原版的
// SetTextAutoSize 渲染第二数字 —— 复用引擎原版主题字号与缩放逻辑，因此加装其他 mod 时
// 不会出现字体被放大的问题。
[HarmonyPatch(typeof(NPower))]
public static class SecondAmountLabelPatch
{
    private static readonly FieldInfo ModelField = AccessTools.Field(typeof(NPower), "_model");
    private static readonly MethodInfo RefreshAmountMethod = AccessTools.Method(typeof(NPower), "RefreshAmount");

    // 复制节点时使用的 flags（与 BaseLib 一致：Signals | Groups | Scripts | UseInstantiation）
    private const int DuplicateFlags = 15;

    private static PowerModel? GetModel(NPower instance)
    {
        return ModelField.GetValue(instance) as PowerModel;
    }

    // 在引擎刷新能力右下角 Amount 标签后，同步刷新右上角第二数字标签
    [HarmonyPatch("RefreshAmount")]
    [HarmonyPostfix]
    private static void ShowSecondAmount(NPower __instance)
    {
        if (!__instance.IsNodeReady() || GetModel(__instance) is not IHasSecondAmount hasSecondAmount)
        {
            return;
        }

        if (!__instance.HasNode("Amount2Label"))
        {
            MegaLabel baseLabel = __instance.GetNode<MegaLabel>("%AmountLabel");
            MegaLabel copy = (MegaLabel)baseLabel.Duplicate(DuplicateFlags);
            copy.Name = "Amount2Label";
            copy.UniqueNameInOwner = false;
            copy.Visible = false;
            __instance.AddChild(copy, forceReadableName: false, Node.InternalMode.Disabled);
            __instance.MoveChild(copy, baseLabel.GetIndex());
        }

        MegaLabel amountLabel = __instance.GetNode<MegaLabel>("%AmountLabel");
        MegaLabel amount2Label = __instance.GetNode<MegaLabel>("Amount2Label");
        string secondAmount = hasSecondAmount.GetSecondAmount();
        if (string.IsNullOrEmpty(secondAmount))
        {
            amount2Label.Visible = false;
            return;
        }

        amount2Label.Visible = true;
        amount2Label.SetTextAutoSize(secondAmount);
        int themeFontSize = amount2Label.GetThemeFontSize(ThemeConstants.Label.FontSize);
        // 放在原右下角标签的正上方，形成“右上角”的第二数字
        amount2Label.Position = amountLabel.Position + new Vector2(0, -(themeFontSize + 2));
    }

    // 节点订阅模型事件时，把能力与它的刷新动作关联起来
    [HarmonyPatch("SubscribeToModelEvents")]
    [HarmonyPostfix]
    private static void Subscribe(NPower __instance)
    {
        if (GetModel(__instance) is IHasSecondAmount power)
        {
            SecondAmountRegistry.Register(power, () => RefreshAmountMethod.Invoke(__instance, null));
        }
    }

    // 节点退订模型事件时解除关联
    [HarmonyPatch("UnsubscribeFromModelEvents")]
    [HarmonyPostfix]
    private static void Unsubscribe(NPower __instance)
    {
        if (GetModel(__instance) is IHasSecondAmount power)
        {
            SecondAmountRegistry.Unregister(power);
        }
    }
}
