using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

/// <summary>眩惑期间仅将意图图标和行动粒子染蓝，数字及原有动画保持原样。</summary>
[HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
public static class BewildermentIntentColorPatch
{
    private sealed class OriginalMaterials
    {
        public Material? Sprite;
        public Material? Particle;
    }
    private static readonly ConditionalWeakTable<NIntent, OriginalMaterials> Originals = new();
    private static ShaderMaterial? _blueMaterial;
    private static ShaderMaterial BlueMaterial => _blueMaterial ??= new ShaderMaterial
    {
        Shader = new Shader
        {
            // 用纹理亮度重新着色，避免直接乘蓝色导致原本红色的意图变黑。
            Code = """
                shader_type canvas_item;
                void fragment() {
                    vec4 pixel = texture(TEXTURE, UV);
                    float brightness = max(pixel.r, max(pixel.g, pixel.b));
                    COLOR = vec4(vec3(0.25, 0.60, 1.0) * brightness, pixel.a) * COLOR;
                }
                """
        }
    };

    public static void Postfix(NIntent __instance, Creature ____owner, Sprite2D ____intentSprite,
        CpuParticles2D ____intentParticle)
    {
        if (____owner is null || ____intentSprite is null || ____intentParticle is null)
            return;
        bool bewildered = ____owner.Side == CombatSide.Enemy &&
            ____owner.GetPower<BewildermentPower>() is { Amount: > 0 };
        if (bewildered)
        {
            if (!Originals.TryGetValue(__instance, out _))
                Originals.Add(__instance, new OriginalMaterials
                {
                    Sprite = ____intentSprite.Material,
                    Particle = ____intentParticle.Material
                });
            ____intentSprite.Material = BlueMaterial;
            ____intentParticle.Material = BlueMaterial;
        }
        else if (Originals.TryGetValue(__instance, out var original))
        {
            ____intentSprite.Material = original.Sprite;
            ____intentParticle.Material = original.Particle;
            Originals.Remove(__instance);
        }
    }
}
