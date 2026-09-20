using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;

using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Patches;

// 休息处选择“锻造”/“休息”时各播放一段语音。
//
// 接入点：两种选项各自的 DoLocalPostSelectVfx —— 本体在选项被选定后调用，且其文档
// 明确“只对该选项的拥有者（本机玩家）执行”，多人下不会替队友发声。
// （不用 OnSelect：它对远端玩家的选择也会触发；不用 Hook.AfterRestSiteHeal/Smith：
//   前者会被多人“疗伤”选项和事件调用，后者无法区分是不是“我自己选了休息”。）
// 音频文件：audios/rest_site_smith.mp3 与 audios/rest_site_rest.mp3。
[HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.DoLocalPostSelectVfx))]
public static class RestSiteSmithSfxPatch
{
    public static void Postfix()
    {
        RestSiteSfx.PlayForLocalCharacter("event:/newsanguo/sfx/rest_site_smith");
    }
}

[HarmonyPatch(typeof(HealRestSiteOption), nameof(HealRestSiteOption.DoLocalPostSelectVfx))]
public static class RestSiteHealSfxPatch
{
    public static void Postfix()
    {
        RestSiteSfx.PlayForLocalCharacter("event:/newsanguo/sfx/rest_site_rest");
    }
}

internal static class RestSiteSfx
{
    // 只在“本机玩家当前用的是新三国角色”时播放：上面两个补丁对所有角色与其它 mod 都生效
    public static void PlayForLocalCharacter(string sfx)
    {
        NRestSiteRoom? room = NRestSiteRoom.Instance;
        if (room is null)
        {
            return;
        }
        foreach (NRestSiteCharacter character in room.Characters)
        {
            if (!LocalContext.IsMe(character.Player))
            {
                continue;
            }
            if (character.Player.Character is NewsanguoCharacter)
            {
                NewsanguoSfx.Play(sfx);
            }
            return;
        }
    }
}
