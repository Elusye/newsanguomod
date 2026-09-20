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
public class ReleasePower : ModPowerTemplate
{
    // 「关羽之歌」音频文件（mp3 经 NewsanguoSfx 统一入口用 Godot AudioStreamPlayer 播放）。
    // 说明：该音效在 FMOD 工程里被设为流式(Streaming)，构建时音频数据不会打进 newsanguo.bank，
    // 且工程从未生成配套的 .stream 文件，导致 FMOD 事件能创建实例但没有任何声音。
    // 因此这里绕开 FMOD，直接播 mp3（游戏自身的 NDebugAudioManager 也是这么做的）。
    private const string SongFilePath = "res://newsanguo/audios/song_of_guan_yu.mp3";

    // 歌曲在总线上的基准音量（dB）。长音频不走 MasterVolumeDb 那套短语音效校准，
    // 也不叠游戏「音效」选项曲线（见 NewsanguoSfx.PlayOwnLevel），沿用早期独立播放器实测可用的 +5 dB；
    // mod 音效总开关 / 倍率滑杆 / 听觉受损门照常生效。
    private const float SongVolumeDb = 5f;

    // 正在播放的播放器，保留句柄以便 SL / 进入下一个房间时打断
    private static AudioStreamPlayer? _songPlayer;

    // 打断“关羽之歌”（由 Entry 在进入主菜单 / 进入新房间时调用）
    public static void StopSongOfGuanyu()
    {
        AudioStreamPlayer? player = _songPlayer;
        _songPlayer = null;
        NewsanguoSfx.Stop(player);
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
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}Big.png"
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

    // 播放「关羽之歌」：统一走 NewsanguoSfx，因此受 mod 音效总开关 / 倍率滑杆 / 听觉受损门控制，
    // 并保留歌曲自己的基准电平（PlayOwnLevel 不叠短语音效那套校准）。
    // 循环由 NewsanguoSfx 维护，直到进入下一个房间 / 回主菜单时被 StopSongOfGuanyu() 打断。
    private static void PlaySongOfGuanyu()
    {
        // 若上一首仍未结束，先停掉，避免句柄被覆盖后无法打断
        StopSongOfGuanyu();
        _songPlayer = NewsanguoSfx.PlayOwnLevel(SongFilePath, SongVolumeDb, loop: true);
    }
}
