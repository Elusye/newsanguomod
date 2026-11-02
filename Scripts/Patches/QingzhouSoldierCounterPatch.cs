using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Combat;
using newsanguo.Scripts.Monsters;

namespace newsanguo.Scripts.Patches;

/// <summary>只读显示所属玩家的存活青州兵，不创建能力或修改战斗状态。</summary>
[HarmonyPatch(typeof(NHealthBar), nameof(NHealthBar.SetCreature))]
public static class QingzhouSoldierCounterPatch
{
    public static void Postfix(NHealthBar __instance, Creature creature)
    {
        if (!creature.IsPlayer || __instance.HpBarContainer is not { } bar)
            return;
        var counter = new QingzhouSoldierCounter { Name = "NewsanguoQingzhouCounter" };
        counter.OwnerCreature = creature;
        counter.BlockTemplate = (Control)AccessTools.Field(typeof(NHealthBar), "_blockContainer").GetValue(__instance)!;
        counter.BlockLabelTemplate = (MegaLabel)AccessTools.Field(typeof(NHealthBar), "_blockLabel").GetValue(__instance)!;
        bar.AddChild(counter);
    }
}

/// <summary>附着血条右端；每帧只遍历自身宠物，避免死亡、离场与召唤事件顺序造成漏更新。</summary>
public partial class QingzhouSoldierCounter : Control
{
    internal Creature OwnerCreature = null!;
    internal Control BlockTemplate = null!;
    internal MegaLabel BlockLabelTemplate = null!;
    private MegaLabel _countLabel = null!;
    private Control _badge = null!;
    private int _lastCount = -1;
    private bool _hovered;

    public override void _Ready()
    {
        // 仅复用原版图标和文字样式，不复制已被其他模组改动的整个格挡容器。
        // BloodCheck 会递归查找 BlockIcon / BlockLabel；独立命名避免数量标记被误移动。
        var originalIcon = BlockTemplate.FindChild("BlockIcon", recursive: true, owned: false) as TextureRect;
        _badge = new Control { Name = "SoldierBadge" };
        AddChild(_badge);
        if (originalIcon is not null)
        {
            var icon = new TextureRect
            {
                Name = "NewsanguoSoldierIcon",
                Texture = originalIcon.Texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SelfModulate = new Color("CC995F")
            };
            _badge.AddChild(icon);
            icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }
        _countLabel = (MegaLabel)BlockLabelTemplate.Duplicate((int)DuplicateFlags.Scripts);
        _countLabel.Name = "NewsanguoSoldierCount";
        _countLabel.UniqueNameInOwner = false;
        _countLabel.Visible = true;
        _countLabel.Modulate = Colors.White;
        _countLabel.SelfModulate = Colors.White;
        _countLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _countLabel.VerticalAlignment = VerticalAlignment.Center;
        _countLabel.Scale = Vector2.One;
        _countLabel.Rotation = 0;
        _badge.AddChild(_countLabel);
        _countLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MakeMouseTransparent(_badge);

        AnchorLeft = AnchorRight = 1f;
        AnchorTop = AnchorBottom = 0.5f;
        Vector2 badgeSize = BlockTemplate.Size;
        OffsetLeft = -badgeSize.X * 0.5f - 12f;
        OffsetRight = badgeSize.X * 0.5f - 12f;
        OffsetTop = -badgeSize.Y * 0.5f;
        OffsetBottom = badgeSize.Y * 0.5f;
        _badge.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        Visible = false;
        RefreshCount();
    }

    private static void MakeMouseTransparent(Node node)
    {
        if (node is Control control)
            control.MouseFilter = MouseFilterEnum.Ignore;
        foreach (Node child in node.GetChildren())
            MakeMouseTransparent(child);
    }

    private void OnMouseEntered()
    {
        _hovered = true;
        ShowHoverTip();
    }

    private void OnMouseExited()
    {
        _hovered = false;
        NHoverTipSet.Remove(this);
    }

    private void ShowHoverTip()
    {
        NHoverTipSet.Remove(this);
        if (_lastCount <= 0)
            return;
        var text = new LocString("cards", "NEWSANGUO_QINGZHOU_COUNTER");
        text.Add("Amount", _lastCount);
        NHoverTipSet.CreateAndShow(this, new HoverTip(text), HoverTip.GetHoverTipAlignment(this));
    }

    public override void _Process(double delta) => RefreshCount();

    private void RefreshCount()
    {
        int count = CountLivingSoldiers(OwnerCreature);
        if (count == _lastCount)
            return;
        _lastCount = count;
        Visible = count > 0;
        _countLabel.SetTextAutoSize(count.ToString());
        if (_hovered)
            ShowHoverTip();
    }

    internal static int CountLivingSoldiers(Creature owner)
    {
        if (!owner.IsAlive || owner.Player?.PlayerCombatState is not { } combat)
            return 0;
        int count = 0;
        foreach (Creature pet in combat.Pets)
            if (pet.Monster is QingzhouSoldier && pet.IsAlive && pet.PetOwner == owner.Player)
                count++;
        return count;
    }
}
