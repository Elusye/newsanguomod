using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using newsanguo.Scripts.Powers;

namespace newsanguo.Scripts.Patches;

/// <summary>仅翻转怪物身体，避免负缩放影响血条、意图位置和判定区域。</summary>
[HarmonyPatch]
public static class BewildermentFacingPatch
{
    private sealed class Facing
    {
        public float OriginalSign;
    }
    private static readonly ConditionalWeakTable<Node2D, Facing> Originals = new();

    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NCreature), "_Ready");
        yield return AccessTools.Method(typeof(NCreature), "UpdatePhobiaMode");
    }
    public static void Postfix(NCreature __instance) => Refresh(__instance);

    internal static void Refresh(Creature creature)
    {
        NCreature? node = NCombatRoom.Instance?.GetCreatureNode(creature);
        if (node is not null)
            Refresh(node);
    }

    private static void Refresh(NCreature node)
    {
        if (!GodotObject.IsInstanceValid(node) ||
            node.Visuals is not { } visuals || !GodotObject.IsInstanceValid(visuals) || !visuals.IsNodeReady())
            return;
        bool flip = node.Entity.Side == CombatSide.Enemy &&
            node.Entity.GetPower<BewildermentPower>() is { Amount: > 0 };
        Apply(visuals.Body, flip);
        Apply(visuals.GetNodeOrNull<Node2D>("%PhobiaModeVisuals"), flip);
    }

    private static void Apply(Node2D? body, bool flip)
    {
        if (body is null || !GodotObject.IsInstanceValid(body))
            return;
        Vector2 scale = body.Scale;
        if (flip)
        {
            if (!Originals.TryGetValue(body, out var facing))
            {
                facing = new Facing { OriginalSign = scale.X < 0 ? -1f : 1f };
                Originals.Add(body, facing);
            }
            body.Scale = new Vector2(-facing.OriginalSign * Mathf.Abs(scale.X), scale.Y);
        }
        else if (Originals.TryGetValue(body, out var facing))
        {
            body.Scale = new Vector2(facing.OriginalSign * Mathf.Abs(scale.X), scale.Y);
            Originals.Remove(body);
        }
    }
}
