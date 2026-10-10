using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using newsanguo.Scripts.Relics;
using newsanguo.Scripts.Settings;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace newsanguo.Scripts.Characters;

[RegisterCharacter]
public class ShuHanCharacter : ModCharacterTemplate<
    ShuHanCardPool,
    NewsanguoRelicPool,
    NewsanguoPotionPool>
{
    // 使用 Ironclad 作为原版占位（动画、音效、场景等）
    public override string PlaceholderCharacterId => ModContentRegistry.VanillaCharacterIds.Ironclad;

    // 本角色没有注册任何 *_EPOCH 模型与解锁规则（NEWSANGUO_CHARACTER_NEWSANGUO_CHARACTER2/3/4_EPOCH、
    // UnlockEpochAfterEliteVictories / UnlockEpochAfterBossVictories / RegisterPostRunCharacterUnlockEpoch 等）。
    // 而 RitsuLib 的 RequiresEpochAndTimeline 为 true 时，兼容补丁会保留“依赖原版 *_EPOCH 的进度路径”，
    // 于是每次里程碑结算都会落空并刷警告（实测日志）：
    //   [Content]    Character timeline: CHARACTER.NEWSANGUO_CHARACTER_NEWSANGUO_CHARACTER RequiresEpochAndTimeline=True
    //   [DebugCompat] Missing epoch 'NEWSANGUO_CHARACTER_NEWSANGUO_CHARACTER2/3/4_EPOCH' ... after Act 1/2/3. Skipping
    //   [Unlocks]    Mod character '...' has no registered elite-win / boss-win / post-run epoch rule. Skipping
    // 即 Act1/2/3 通关、精英、Boss、通关后的解锁进度全部是空转。这里显式声明不需要（与原版孙乾 mod 一致），
    // RitsuLib 便不再为本角色保留这些原版进度路径；若日后要做解锁时间线，改成 true 并补齐上述 epoch 与规则。
    public override bool RequiresEpochAndTimeline => false;

    // —— “测试模式”门禁（设置页：设置 → Mod 设置 → 新三国设置 → 测试模式）——
    // 未开启测试模式时，蜀汉从原版角色选择界面隐藏，也不会被“随机角色”选中。
    // RitsuLib 用 IModCharacterVanillaSelectionPolicy 实现这件事：它在 InitCharacterButtons /
    // NCharacterSelectButton.Init / UpdateRandomCharacterVisibility / StartRunLobby.BeginRunLocally
    // 这几个方法外挂作用域补丁，并在 ModelDb.AllCharacters 的 getter 上按作用域过滤
    // （CharacterVanillaSelectionPolicyScope.Apply）。注意与“解锁态”无关：走 UnlockState 那条路会让
    // 角色以“锁定”样子留在选人界面，而且 UpdateRandomCharacterVisibility 要求全员解锁才显示
    // “随机角色”按钮，会连带把随机按钮永久藏掉，所以用这套策略属性而不是 epoch 解锁。
    // 这两个属性在每次构建选人界面时读取（非模型注册期烘焙）⇒ 改开关后重进选人界面即可生效，不必重启。
    // 卡牌总览（图鉴）的蜀汉卡池筛选项保持可见：测试模式关掉时仍允许查阅卡牌，故不覆写
    // HideInCardLibraryCompendium。
    public override bool HideFromVanillaCharacterSelect => !NewsanguoTestModeSettings.TestModeEnabled;

    public override bool AllowInVanillaRandomCharacterSelect => NewsanguoTestModeSettings.TestModeEnabled;

    public override CharacterAssetProfile AssetProfile => new(
        Ui: new CharacterUiAssetSet(
            IconTexturePath: "res://newsanguo/images/characters/ShuHan/icon.png",
            IconOutlineTexturePath: "res://newsanguo/images/characters/ShuHan/iconOutline.png",
            IconPath: "res://newsanguo/images/characters/ShuHan/icon.png",
            // 选人背景：不再直接给裸 PNG（RitsuLib 的 Texture2D 分支只能铺一张满屏图），
            // 改为给场景，走 RitsuLib 的 PackedScene 分支（照孙乾 mod 的套路），
            // 之后要加多层/动画元素时直接在 scenes/shuhan_bg.tscn 里加节点即可。
            CharacterSelectBgPath: "res://scenes/shuhan_bg.tscn",
            CharacterSelectIconPath: "res://newsanguo/images/characters/ShuHan/character_select_icon.png",
            CharacterSelectLockedIconPath: "res://newsanguo/images/characters/ShuHan/character_select_locked_icon.png",
            CharacterSelectTransitionPath: null,
            MapMarkerPath: "res://newsanguo/images/characters/ShuHan/map_marker.png"
        ),
        Scenes: new CharacterSceneAssetSet(
            VisualsPath: "res://newsanguo/images/characters/ShuHan/combat_body.png"
        ),
        // 选人/死亡音效：仍以 event:/ 路径经引擎入口触发，EngineSfxRedirectPatch 会截获
        // 这些路径并转交 NewsanguoSfx 播放同名音频资源（newsanguo.bank 已删除，不经过 FMOD）。
        // 选人音效按角色区分：蜀汉播 res://newsanguo/audios/character_select_shuhan.mp3；
        // 该文件还没放进包时退回通用 event:/newsanguo/sfx/character_select（见 EngineSfxRedirectPatch.Fallbacks）。
        Audio: new CharacterAudioAssetSet(
            CharacterSelectSfx: "event:/newsanguo/sfx/character_select_shuhan",
            CharacterTransitionSfx: null,
            AttackSfx: null,
            CastSfx: null,
            // 角色死亡音效：PNG 视觉下由 PlayerDeathSfxPatch 在 StartDeathAnim 后缀补播，
            // 直接走 NewsanguoSfx；此处事件路径供引擎级入口匹配。
            DeathSfx: "event:/newsanguo/sfx/character_death"
        ),
        // 多人宝箱石头剪刀布手势图（当前为占位图，可后续替换）：
        // 原版默认按角色 id 查找 res://images/ui/hands/multiplayer_hand_{id}_{gesture}.png，
        // 按角色分别配置独立目录，当前沿用同一套占位图，可单独替换而不影响另一角色。
        Multiplayer: new CharacterMultiplayerAssetSet(
            ArmPointingTexturePath: "res://newsanguo/images/ui/hands/ShuHan/multiplayer_hand_point.png",
            ArmRockTexturePath: "res://newsanguo/images/ui/hands/ShuHan/multiplayer_hand_rock.png",
            ArmPaperTexturePath: "res://newsanguo/images/ui/hands/ShuHan/multiplayer_hand_paper.png",
            ArmScissorsTexturePath: "res://newsanguo/images/ui/hands/ShuHan/multiplayer_hand_scissors.png"
        ),
        // 死亡/商店/休息处形象（当前为占位图，可后续替换）
        // 线索键用引擎的动画名（AnimState：die / idle_loop …）。
        // RitsuLib 的非 Spine 标准状态图只有 idle（必填、循环）/ dead / hit / attack / cast / relaxed
        // 这几个动画位，**没有 revive 位**：本体的 Revive 触发器在标准图里就是“切回 idle”。
        // 而 RitsuLib 会拒绝进入“后端没有对应线索的状态”（记警告 + 保持当前状态不变），
        // 所以当初只定义 die 时：死亡能进 Dead（有 die 线索），复活想回 idle 却没有 idle 线索可进，
        // 状态机卡在 Dead —— 表现就是多人 1 血复活后形象仍是死亡形象。
        // 因此这里给出 idle（含别名 idle_loop）对应的贴图，指回战斗形象。
        VisualCues: VisualCueSetBuilder.Create()
            .Single("die", "res://newsanguo/images/characters/ShuHan/death_body.png")
            .Single("idle", "res://newsanguo/images/characters/ShuHan/combat_body.png")
            .Single("idle_loop", "res://newsanguo/images/characters/ShuHan/combat_body.png")
            .Build(),
        WorldProceduralVisuals: CharacterWorldProceduralVisualSetBuilder.Create()
            .Merchant(builder => builder
                .Single("idle", "res://newsanguo/images/characters/ShuHan/merchant_body.png")
                .Single("relaxed", "res://newsanguo/images/characters/ShuHan/merchant_body.png"))
            .RestSite(builder => builder
                .Single("relaxed", "res://newsanguo/images/characters/ShuHan/rest_site_body.png")
                .Single("idle", "res://newsanguo/images/characters/ShuHan/rest_site_body.png"))
            .Build(),
        // 美味饼干等原版遗物的角色专属图标（当前为占位图，可后续替换）
        VanillaRelicVisualOverrides:
        [
            new CharacterVanillaRelicVisualOverride("yummy_cookie", new RelicAssetProfile(
                IconPath: "res://newsanguo/images/relics/YummyCookie.png",
                IconOutlinePath: "res://newsanguo/images/relics/YummyCookieOutline.png",
                BigIconPath: "res://newsanguo/images/relics/YummyCookieBig.png"
            ))
        ]
    );

    public override int StartingHp => 75;
    public override int MaxEnergy => 3;
    public override int StartingGold => 99;

    public override CharacterGender Gender => CharacterGender.Masculine;

    public override Color NameColor => StsColors.red;

    public override float AttackAnimDelay => 0.15f;

    public override float CastAnimDelay => 0.25f;

    public override Color EnergyLabelOutlineColor => new Color("801212FF");

    public override Color DialogueColor => new Color("590700");

    public override VfxColor SpeechBubbleColor => VfxColor.Red;

    public override Color MapDrawingColor => new Color("6B492E");

    public override Color RemoteTargetingLineColor => new Color("E15847FF");

    public override Color RemoteTargetingLineOutline => new Color("801212FF");

    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}
