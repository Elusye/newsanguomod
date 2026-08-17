using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “释怀”：战斗结束时，回复 Amount 点生命并播放「关羽之歌」。
/// </summary>
[RegisterPower]
public class release_power : ModPowerTemplate
{
    // 「关羽之歌」音频文件（mp3 直接由 Godot 播放）。
    // 说明：该音效在 FMOD 工程里被设为流式(Streaming)，构建时音频数据不会打进 newsanguo.bank，
    // 且工程从未生成配套的 .stream 文件，导致 FMOD 事件能创建实例但没有任何声音。
    // 因此这里绕开 FMOD，用 Godot 的 AudioStreamPlayer 播放 mp3（游戏自身的 NDebugAudioManager 也是这么做的）。
    private const string SongFilePath = "res://newsanguo/audios/song_of_guan_yu.mp3";

    // 正在播放的 Godot 音频播放器，保留句柄以便 SL / 进入下一个房间时打断
    private static AudioStreamPlayer? _songPlayer;

    // 打断“关羽之歌”（由 Entry 在进入主菜单 / 进入新房间时调用）
    public static void StopSongOfGuanyu()
    {
        AudioStreamPlayer? player = _songPlayer;
        _songPlayer = null;
        if (player is null || !GodotObject.IsInstanceValid(player))
        {
            return;
        }

        player.Stop();
        player.QueueFree();
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 叠加方式：计数器，Amount 表示战斗结束时的回复量
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 战斗结束钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    // 战斗结束时：回复 Amount 点生命并播放「关羽之歌」
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner is null || !Owner.IsAlive)
        {
            return;
        }

        PlaySongOfGuanyu();
        await CreatureCmd.Heal(Owner, Amount);
    }

    // 用 Godot AudioStreamPlayer 播放「关羽之歌」（挂在场景树根节点，走 SFX 总线）。
    // 与游戏内置 NDebugAudioManager 的播放方式一致；保留句柄以便后续 stop()。
    private static void PlaySongOfGuanyu()
    {
        // 若上一首仍未结束，先停掉，避免句柄被覆盖后无法打断
        StopSongOfGuanyu();

        // 优先走 Godot 资源系统（自动处理导入/remap），失败再直接按原始文件读取
        AudioStream? stream = ResourceLoader.Load<AudioStream>(SongFilePath) ?? AudioStreamMP3.LoadFromFile(SongFilePath);
        if (stream is null || Engine.GetMainLoop() is not SceneTree tree)
        {
            return;
        }

        AudioStreamPlayer player = new AudioStreamPlayer
        {
            Stream = stream,
            Bus = "SFX",
            // 相对默认音量上调 5dB
            VolumeDb = 5.0f
        };

        tree.Root.AddChild(player);
        player.Play();
        _songPlayer = player;

        // 歌曲播放完正常时长后自动循环，直到进入下一个房间 / 回主菜单时被 StopSongOfGuanyu() 打断
        player.Finished += () =>
        {
            if (!GodotObject.IsInstanceValid(player) || _songPlayer != player)
            {
                return;
            }
            player.Play();
        };
    }
}
