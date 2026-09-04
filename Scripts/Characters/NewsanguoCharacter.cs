using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using newsanguo.Scripts.Relics;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Characters.Visuals.Definition;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Visuals.Definition;

namespace newsanguo.Scripts.Characters;

[RegisterCharacter]
public class NewsanguoCharacter : ModCharacterTemplate<
    NewsanguoCardPool,
    NewsanguoRelicPool,
    NewsanguoPotionPool>
{
    // 使用 Ironclad 作为原版占位（动画、音效、场景等）
    public override string PlaceholderCharacterId => ModContentRegistry.VanillaCharacterIds.Ironclad;

    public override CharacterAssetProfile AssetProfile => new(
        Ui: new CharacterUiAssetSet(
            IconTexturePath: "res://newsanguo/images/characters/Newsanguo/icon.png",
            IconOutlineTexturePath: "res://newsanguo/images/characters/Newsanguo/icon_outline.png",
            IconPath: "res://newsanguo/images/characters/Newsanguo/icon.png",
            CharacterSelectBgPath: "res://newsanguo/images/characters/Newsanguo/character_select_bg.png",
            CharacterSelectIconPath: "res://newsanguo/images/characters/Newsanguo/character_select_icon.png",
            CharacterSelectLockedIconPath: "res://newsanguo/images/characters/Newsanguo/character_select_locked_icon.png",
            CharacterSelectTransitionPath: null,
            MapMarkerPath: "res://newsanguo/images/characters/Newsanguo/map_marker.png"
        ),
        Scenes: new CharacterSceneAssetSet(
            VisualsPath: "res://newsanguo/images/characters/Newsanguo/combat_body.png"
        ),
        // 选人/死亡音效：仍以 event:/ 路径经引擎入口触发，EngineSfxRedirectPatch 会截获
        // 这两个路径并转交 NewsanguoSfx 播放同名音频资源（newsanguo.bank 已删除，不经过 FMOD）。
        Audio: new CharacterAudioAssetSet(
            CharacterSelectSfx: "event:/newsanguo/sfx/character_select",
            CharacterTransitionSfx: null,
            AttackSfx: null,
            CastSfx: null,
            // 角色死亡音效：PNG 视觉下由 PlayerDeathSfxPatch 在 StartDeathAnim 后缀补播，
            // 直接走 NewsanguoSfx；此处事件路径供引擎级入口匹配。
            DeathSfx: "event:/newsanguo/sfx/character_death"
        ),
        // 多人宝箱石头剪刀布手势图（当前为占位图，可后续替换）：
        // 原版默认按角色 id 查找 res://images/ui/hands/multiplayer_hand_{id}_{gesture}.png，
        // 此处改为 mod 自有路径，避免宝箱手势图缺失。
        Multiplayer: new CharacterMultiplayerAssetSet(
            ArmPointingTexturePath: "res://newsanguo/images/ui/hands/multiplayer_hand_point.png",
            ArmRockTexturePath: "res://newsanguo/images/ui/hands/multiplayer_hand_rock.png",
            ArmPaperTexturePath: "res://newsanguo/images/ui/hands/multiplayer_hand_paper.png",
            ArmScissorsTexturePath: "res://newsanguo/images/ui/hands/multiplayer_hand_scissors.png"
        ),
        // 死亡/商店/休息处形象（当前为占位图，可后续替换）
        VisualCues: VisualCueSetBuilder.Create()
            .Single("die", "res://newsanguo/images/characters/Newsanguo/death_body.png")
            .Build(),
        WorldProceduralVisuals: CharacterWorldProceduralVisualSetBuilder.Create()
            .Merchant(builder => builder
                .Single("idle", "res://newsanguo/images/characters/Newsanguo/merchant_body.png")
                .Single("relaxed", "res://newsanguo/images/characters/Newsanguo/merchant_body.png"))
            .RestSite(builder => builder
                .Single("relaxed", "res://newsanguo/images/characters/Newsanguo/rest_site_body.png")
                .Single("idle", "res://newsanguo/images/characters/Newsanguo/rest_site_body.png"))
            .Build(),
        // 美味饼干等原版遗物的角色专属图标（当前为占位图，可后续替换）
        VanillaRelicVisualOverrides:
        [
            new CharacterVanillaRelicVisualOverride("yummy_cookie", new RelicAssetProfile(
                IconPath: "res://newsanguo/images/relics/yummy_cookie.png",
                IconOutlinePath: "res://newsanguo/images/relics/yummy_cookie_outline.png",
                BigIconPath: "res://newsanguo/images/relics/yummy_cookie_big.png"
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
