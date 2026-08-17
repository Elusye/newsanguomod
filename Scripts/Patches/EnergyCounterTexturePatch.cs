using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Patches;

// 战斗 HUD 能量计数器（NEnergyCounter）的场景路径由角色 Id 决定（res://scenes/combat/energy_counters/{id}_energy_counter.tscn），
// 新三国角色沿用铁甲战士占位 Id，因此战斗中的能量球纹理是铁甲战士的。
// 由于 EnergyCounterPath 是非虚属性无法覆写、mod 也无法覆盖 res://scenes/... 的原版场景，
// 这里在能量计数器 Ready 后把球体各图层纹理替换为 mod 自有的能量图标。
[HarmonyPatch(typeof(NEnergyCounter), "_Ready")]
public static class NewsanguoEnergyCounterPatch
{
    private const string ModIconPath = "res://newsanguo/images/ui/energy_newsanguo.png";

    // 注意：不能使用 ___player 注入——原版字段名为 _player（带下划线），
    // 当前 Harmony 版本对 ___ 参数只匹配不带下划线的字段名，会导致补丁应用失败。
    public static void Postfix(NEnergyCounter __instance)
    {
        try
        {
            Player? player = AccessTools.Field(typeof(NEnergyCounter), "_player").GetValue(__instance) as Player;
            if (player?.Character is not NewsanguoCharacter)
            {
                return;
            }

            Texture2D? icon = Godot.ResourceLoader.Load<Texture2D>(ModIconPath);
            if (icon is null)
            {
                Diagnostics.Log($"[NewsanguoEnergyCounter] 能量图标加载失败: {ModIconPath}");
                return;
            }

            // 替换球体各图层纹理：Layers 下的直接子节点（Layer1/4/5）与 RotationLayers 内的旋转图层（Layer2/3）
            // 原版各层是图集的不同区域（组合成完整能量球），替换为同一张完整图标后，
            // 旋转层会出现"相同图层旋转重影"——因此将旋转图层组整体隐藏，只保留静态层显示完整图标。
            Control layers = __instance.GetNode<Control>("%Layers");
            int replaced = ReplaceLayerTextures(layers, icon);
            Control? rotationLayers = layers.GetNodeOrNull<Control>("RotationLayers");
            if (rotationLayers is not null)
            {
                rotationLayers.Visible = false;
            }
            Diagnostics.Log($"[NewsanguoEnergyCounter] 替换能量球纹理完成，共替换 {replaced} 个图层，旋转图层组已隐藏");
        }
        catch (Exception e)
        {
            Diagnostics.Log($"[NewsanguoEnergyCounter] 替换能量球纹理失败: {e}");
        }
    }

    private static int ReplaceLayerTextures(Control? parent, Texture2D icon)
    {
        if (parent is null)
        {
            return 0;
        }
        int count = 0;
        foreach (TextureRect layer in parent.GetChildren().OfType<TextureRect>())
        {
            layer.Texture = icon;
            count++;
        }
        return count;
    }
}
