using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “释怀”：战斗结束时，回复 Amount 点生命并播放「关羽之歌」
/// （90% 原曲，10% 播放 sp1~sp4 变种，四个变种各 2.5%）。
/// </summary>
[RegisterPower]
public class ReleasePower : ModPowerTemplate
{
    // 「关羽之歌」音频文件（mp3 经 NewsanguoSfx 统一入口用 Godot AudioStreamPlayer 播放）。
    // 说明：该音效在 FMOD 工程里被设为流式(Streaming)，构建时音频数据不会打进 newsanguo.bank，
    // 且工程从未生成配套的 .stream 文件，导致 FMOD 事件能创建实例但没有任何声音。
    // 因此这里绕开 FMOD，直接播 mp3（游戏自身的 NDebugAudioManager 也是这么做的）。
    private const string SongFilePath = "res://newsanguo/audios/song_of_guan_yu.mp3";

    // 变种曲：song_of_guan_yu_sp_1 ~ sp_4（同目录，命名固定为 song_of_guan_yu_sp_{n}.mp3）。
    // 播放分布：90% 原曲，10% 变种（4 个变种均分，各 2.5%）。
    private const string SongVariantPathFormat = "res://newsanguo/audios/song_of_guan_yu_sp_{0}.mp3";
    private const int SongVariantCount = 4;
    // 变种占比（百分比）：10%
    private const uint VariantChancePercent = 10;

    // 抽取本次要播的曲目：90% 原曲，10% 变种（1..4 均匀 → 各 2.5%）。
    //
    // 联机必须两台机器听到同一首，所以这里不能用本机随机数（各机器各抽一次就会放出不同的歌）。
    // 改为「确定性抽样」：用「本局随机种子 + 当前总层数」拼 key，取游戏自带的
    // StringHelper.GetDeterministicHashCode（跨进程稳定，0.107 源码 StringHelper.cs:120）。
    // 这两个输入在联机里每台机器完全一致，所以算出的曲目一致；且它不消耗任何随机数，
    // 因此也不会扰动原版（以及其它 mod）的确定性随机序列。
    // key 里刻意不带玩家槽位 / 不区分是哪个「释怀」实例：同一场战斗结束时的多次调用必然算出同一首，
    // 这样即使多人都带「释怀」，谁最后触发都还是同一首，两端不会因为遍历顺序而分叉。
    private static string PickSongPath(IRunState? runState)
    {
        // 拿不到 runState（理论上不该发生）时退回原曲，保证仍能出声
        if (runState is null)
        {
            return SongFilePath;
        }

        uint mixed = Mix(unchecked((uint)StringHelper.GetDeterministicHashCode(
            $"newsanguo|song_of_guan_yu|{runState.Rng.Seed}|{runState.TotalFloor}")));

        // 0..99 里后 90 个值给原曲 → 90%；前 10 个值再均分给 4 个变种 → 各 2.5%
        if (mixed % 100u >= VariantChancePercent)
        {
            return SongFilePath;
        }
        return string.Format(SongVariantPathFormat, (int)(mixed / 100u % (uint)SongVariantCount) + 1);
    }

    // MurmurHash3 的 fmix32 终混。
    // 必要性：StringHelper.GetDeterministicHashCode 对「只差最后一两个字符」的字符串
    // （本层数 1 / 2 / 3 …）低位几乎不变，直接取模会偏得离谱——实测某些种子下变种率只有 0.2%，
    // 且 4 个变种里只有 1 个会出现。先做一次雪崩混合再取比特，分布才是 90% / 各 2.5%
    // （本地按 6 个种子、3000 场战斗模拟：总变种率 9.93%，四个变种 63/76/84/75）。
    private static uint Mix(uint x)
    {
        x ^= x >> 16;
        x *= 0x85ebca6bu;
        x ^= x >> 13;
        x *= 0xc2b2ae35u;
        x ^= x >> 16;
        return x;
    }

    // 歌曲在总线上的基准音量（dB）。长音频不走 MasterVolumeDb 那套短语音效校准，
    // 也不叠游戏「音效」选项曲线（见 NewsanguoSfx.PlayOwnLevel），沿用早期独立播放器实测可用的 +5 dB；
    // mod 音效总开关 / 倍率滑杆 / 听觉受损门照常生效。
    private const float SongVolumeDb = 5f;

    // 正在播放的播放器 + 它对应的曲目，保留句柄以便 SL / 进入下一个房间时打断
    private static AudioStreamPlayer? _songPlayer;
    private static string? _songPath;

    // 打断“关羽之歌”（由 Entry 在进入主菜单 / 进入新房间时调用）
    public static void StopSongOfGuanyu()
    {
        AudioStreamPlayer? player = _songPlayer;
        _songPlayer = null;
        _songPath = null;
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

        PlaySongOfGuanyu(PickSongPath(Owner.Player?.RunState));
        await CreatureCmd.Heal(Owner, Amount);
    }

    // 播放「关羽之歌」：统一走 NewsanguoSfx，因此受 mod 音效总开关 / 倍率滑杆 / 听觉受损门控制，
    // 并保留歌曲自己的基准电平（PlayOwnLevel 不叠短语音效那套校准）。
    // 循环由 NewsanguoSfx 维护，直到进入下一个房间 / 回主菜单时被 StopSongOfGuanyu() 打断。
    // 曲目由调用方经 PickSongPath() 算好（90% 原曲 / 10% 变种，联机两端同曲），变种同样受打断。
    private static void PlaySongOfGuanyu(string songPath)
    {
        // 同一首已经在播时不要重起：多名玩家都带「释怀」时，本方法会在同一场战斗结束时被调用多次
        // （Hook.AfterCombatEnd 会为该场战斗里所有玩家的实例各跑一次），每次都 Stop + 起播就会让歌曲
        // 从头重放——只要中间有监听者在 AfterCurrentHpChanged 里 await 动画就会听得见前奏重放。
        // 同曲且句柄仍在播 → 直接返回，保证「一场战斗结束只起播一次」，且与钩子间隔长短无关。
        if (_songPlayer is { } current && _songPath == songPath
            && GodotObject.IsInstanceValid(current) && current.Playing)
        {
            return;
        }

        // 换了曲目，或上一首的句柄已失效：先停掉，避免句柄被覆盖后无法打断
        StopSongOfGuanyu();
        _songPlayer = NewsanguoSfx.PlayOwnLevel(songPath, SongVolumeDb, loop: true);
        // 播放器为 null（mod 音效总开关关闭 / 资源缺失）时不记录曲目，下次调用仍会尝试重新起播
        _songPath = _songPlayer is null ? null : songPath;
    }
}
