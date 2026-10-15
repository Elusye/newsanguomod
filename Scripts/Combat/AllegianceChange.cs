using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Assets;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Patches;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.RunData;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Combat;

public sealed class AllegianceChangeState
{
    public bool PendingCaoWei { get; set; }
}

/// <summary>单人局角色转换；保留 Player、牌组、财产和随机数状态，原版角色 ID 随存档保存。</summary>
internal static class AllegianceChange
{
    private static PlayerRunSavedData<AllegianceChangeState> _saved = null!;
    private static readonly FieldInfo CharacterField = AccessTools.Field(typeof(Player), "<Character>k__BackingField")
        ?? throw new MissingFieldException(typeof(Player).FullName, "<Character>k__BackingField");

    internal static void Register() => _saved = RunSavedDataStore.For(Entry.ModId)
        .RegisterPerPlayer<AllegianceChangeState>("allegiance_change");

    internal static void Schedule(Player player)
    {
        if (player.RunState.Players.Count == 1 && player.Character is ShuHanCharacter)
            _saved.Modify(player, state => state.PendingCaoWei = true);
    }

    internal static async Task Complete(IRunState runState)
    {
        if (runState is not RunState concrete || runState.Players.Count != 1) return;
        Player player = runState.Players[0];
        if (!_saved.TryGet(concrete, player.NetId, out var state) || !state.PendingCaoWei) return;
        if (player.Character is ShuHanCharacter)
        {
            // 修改实例字段，不重建 Player；序列化的 CharacterId 会自然变为曹魏。
            CharacterModel target = ModelDb.Character<CaoWeiCharacter>();
            // 预载新角色的局内场景，保留旧角色资源供当前战斗界面完成退出。
            await PreloadManager.LoadRunAssets(new[] { player.Character, target });
            CharacterField.SetValue(player, target);
            Diagnostics.Log("[SwitchAllegiance] 蜀汉已永久转换为曹魏。");
            RefreshUi(player);
        }
        _saved.Modify(player, value => value.PendingCaoWei = false);
    }

    private static void RefreshUi(Player player)
    {
        // 外观加载失败不影响逻辑角色和存档，下个房间会按新角色重新创建。
        try
        {
            if (NRun.Instance is not { } run) return;
            var portrait = run.GlobalUi.TopBar.Portrait;
            foreach (Node child in portrait.GetChildren())
            {
                portrait.RemoveChild(child);
                child.QueueFree();
            }
            portrait.Initialize(player);
            run.GlobalUi.TopBar.PortraitTip.Initialize(player.RunState);
            RefreshMapMarkers(run, player);
        }
        catch (Exception exception)
        {
            Diagnostics.Log($"[SwitchAllegiance] 界面刷新失败：{exception}");
        }
    }

    private static void RefreshMapMarkers(Node node, Player player)
    {
        if (node.GetType().Name == "NMapMarker" && node is TextureRect marker)
            marker.Texture = player.Character.MapMarker;
        foreach (Node child in node.GetChildren()) RefreshMapMarkers(child, player);
    }
}
