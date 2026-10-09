using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

using newsanguo.Scripts.Monsters;

namespace newsanguo.Scripts.Patches;

/// <summary>
/// 「青州兵」的战斗表现修复（用户 2026-10-09 m01230：「召唤的位置很诡异，鼠标悬浮上去也无法看到它的名字」）。
///
/// 【症状②：悬浮看不到名字】引擎只给**原版奥斯提**留了专用分支，其余带 PetOwner 的召唤物走通用宠物行
/// <c>NCombatRoom.AddCreature</c>（.decompile/sts2full/sts2.decompiled.cs:406994-407040）：
///   · 原版奥斯提：:407023-407027 `if (creature.Monster is Osty &amp;&amp; LocalContext.IsMe(player))`
///     ⇒ `OstyScaleToSize(...)` 后 `MoveChildSafely(nCreature, creatureNode.GetIndex())` 然后 return（画在主人后面，交互保留）；
///   · 通用宠物：:407029-407035 摆成一排，并逐只 `nCreature2.ToggleIsInteractable(on: false)`。
/// 那个 false 会把**该玩家所有宠物**（含刚召唤的这只，也含先前的青州兵）的鼠标交互关掉：
/// <c>ToggleIsInteractable</c>（:477065-477071）同时设置 `Hitbox.MouseFilter = Ignore` 与 `_stateDisplay.Visible = false`，
/// 于是 <c>NCreature.OnFocus</c>（:476860-476885）永远不触发 ⇒ 既不 `ShowNameplate()`（名牌「青州兵（迫真）」）
/// 也不显示 <c>Creature.HoverTips</c>（身上的「替死」等能力提示），连血条容器一起被隐藏。
/// 原版非死灵缚者的宠物本来就 `IsHealthBarVisible => false`（鸟屁虫 :111503 / 佩尔军团 :117306），所以这一手对它们无害；
/// 青州兵 `IsHealthBarVisible => base.Creature.IsAlive`（Scripts/Monsters/QingzhouSoldier.cs:57）想要血条与名字，于是被连带阉掉。
/// 修法：引擎每次召唤都对我们执行 false，那我们就每次召唤后把青州兵的交互恢复成 true。
///
/// 【症状①：召唤位置诡异】站位完全由 <c>AddCreature</c> 的公式决定（整场重排 <c>PositionPlayersAndPets</c>
/// 只在战斗建立时跑一次，调用点 :406744）。通用宠物行的公式是
/// `x = 主人x − 20 + i × (主人判定框宽 / (只数−1)) + 自身判定框宽 / 2`、`y = 主人y + 10`
/// ——间距按**主人**的宽度分摊、且每只都向右偏半个自身宽度：青州兵用奥斯提的形象（判定框 232×204），
/// 而曹操/刘备的立绘只有 265×209 宽（`images/characters/*/combat_body.png` 241×190，经 RitsuLib 图片工厂 ×1.1），
/// 于是第 1 只正好压在主人右半身、第 3 只起互相重叠成一坨。
///
/// 本补丁**不另发明站位**，锚点直接复用原版奥斯提的 <c>NCreature.GetOstyOffsetFromPlayer</c>
/// （:477307-477311 = 主人判定框右侧 + `Osty.MinOffset/MaxOffset` 抬高 75 像素）——只有一只时与
/// 「原版 1 生命值奥斯提」站在完全相同的位置。多只时的错开方式照抄创意工坊 mod「召唤独立 / SummonOstys」
/// （workshop id 3768978847，其 <c>OstyPositioning.Reposition</c>）：每只沿**斜向阶梯**再偏
/// <see cref="OffsetX"/> 向右、<see cref="OffsetY"/> 向上（y 轴向下为正），第 <see cref="MaxUniquePositions"/> 只
/// 之后循环复用槽位，越早召唤的越贴近主人。它靠「往上叠」给后续召唤腾位置，既不会挤进敌人区，
/// 也不会像引擎的通用宠物行那样在数量变多时把间距压缩到重叠。
/// 原版奥斯提按血量缩放（`OstyScaleToSize`，:477282-477305）对 1 生命值的青州兵是空操作
/// （`Lerp(1, 2, 1/150) ≈ 1.007`），所以这里不改缩放，青州兵与 1 生命值奥斯提同尺寸。顺带记一笔：
/// 该方法 :477297-477300 会在本地玩家身上把 `position` 补间回 `GetOstyOffsetFromPlayer`，
/// 将来若要对青州兵做缩放必须避开这一句（那个 mod 正是为此把整个方法重写了）。
/// </summary>
public static class SoldierFormation
{
    // 多只时的错开步长：照抄 mod「召唤独立 / SummonOstys」的 OstyPositioning（斜向阶梯：每只向右 30、向上 23）
    private const float OffsetX = 30f;
    private const float OffsetY = -23f;

    // 斜向阶梯共 15 个槽位，第 16 只起循环复用（与那个 mod 的 MaxUniquePositions 一致）
    private const int MaxUniquePositions = 15;

    /// <summary>按「主人 + PlayerCombatState.Pets（入场顺序）」重排他名下的青州兵（召唤后走这条）。</summary>
    public static void LayoutOwner(NCombatRoom room, Player owner)
    {
        if (owner.PlayerCombatState is not { } combatState)
        {
            return;
        }

        List<Creature> soldiers = combatState.Pets
            .Where(pet => pet.Monster is QingzhouSoldier && pet.IsAlive)
            .ToList();
        if (soldiers.Count == 0)
        {
            return;
        }

        List<NCreature> nodes = soldiers
            .Select(soldier => room.GetCreatureNode(soldier))
            .OfType<NCreature>()
            .ToList();
        Layout(room.GetCreatureNode(owner.Creature), nodes);
    }

    /// <summary>
    /// 按「已经在场上的节点」重排：整场重排 <c>PositionPlayersAndPets</c>（战斗建立/读档恢复）会把青州兵
    /// 按通用宠物行重新摆一遍，所以那条路径结束时也要再摆一次，否则恢复存档后站位又变回原样。
    /// </summary>
    public static void LayoutNodes(IEnumerable<NCreature> creatureNodes)
    {
        if (NCombatRoom.Instance is not { } room)
        {
            return;
        }

        List<IGrouping<Creature, NCreature>> groups = creatureNodes
            .Where(node => node.Entity.Monster is QingzhouSoldier && node.Entity.PetOwner is not null && node.Entity.IsAlive)
            .GroupBy(node => node.Entity.PetOwner!.Creature)
            .ToList();
        foreach (IGrouping<Creature, NCreature> group in groups)
        {
            Layout(room.GetCreatureNode(group.Key), group.ToList());
        }
    }

    private static void Layout(NCreature? ownerNode, List<NCreature> soldiers)
    {
        if (ownerNode is null || soldiers.Count == 0)
        {
            return;
        }

        // ① 恢复鼠标交互：引擎的通用宠物行每次都把「非奥斯提」宠物（含之前召的青州兵）关掉
        foreach (NCreature soldier in soldiers)
        {
            soldier.ToggleIsInteractable(true);
        }

        // ② 站位：锚点照抄原版奥斯提（主人右侧 + 抬高 75 像素），多只时沿斜向阶梯一只比一只更右上
        Vector2 anchor = NCreature.GetOstyOffsetFromPlayer(soldiers[0].Entity);

        for (int i = 0; i < soldiers.Count; i++)
        {
            int slot = i % MaxUniquePositions;
            Vector2 offset = new(OffsetX * slot, OffsetY * slot);
            soldiers[i].Position = ownerNode.Position + anchor + offset;
        }
    }
}

/// <summary>召唤青州兵时立刻重排阵型（<c>CreatureCmd.Add</c> → <c>NCombatRoom.AddCreature</c>）。</summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class CaoArtOfWarSoldierFormationPatch
{
    public static void Postfix(NCombatRoom __instance, Creature creature)
    {
        if (creature.Monster is not QingzhouSoldier)
        {
            return;
        }

        Player? owner = creature.PetOwner;
        if (owner is null)
        {
            return;
        }

        SoldierFormation.LayoutOwner(__instance, owner);
    }
}

/// <summary>战斗建立/读档恢复时的整场重排之后，把青州兵的阵型再摆回原版奥斯提那一套。</summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.PositionPlayersAndPets))]
public static class CaoArtOfWarSoldierFormationRelayoutPatch
{
    public static void Postfix(List<NCreature> creatureNodes)
    {
        SoldierFormation.LayoutNodes(creatureNodes);
    }
}
