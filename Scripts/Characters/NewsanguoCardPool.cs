using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

using newsanguo.Scripts.Cards;

namespace newsanguo.Scripts.Characters;

[RegisterSharedCardPool]
public class NewsanguoCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    public override string Title => "newsanguo";

    public override string EnergyColorName => "newsanguo_caowei";

    public override string CardFrameMaterialPath => "card_frame_newsanguo";

    public override Color DeckEntryCardColor => new Color("244C73");

    public override Color EnergyOutlineColor => new Color("4A2F1C");

    // 能量图标路径（RitsuLib 官方覆盖）：
    // - BigEnergyIconPath：EnergyIconHelper.GetPath 的大图标（卡面/遗物/药水费用图标等）
    // - TextEnergyIconPath：卡牌描述 {Energy:energyIcons()} 富文本图标（24x24 小图，避免 128x128 源图渲染/测量失真）
    public override string? BigEnergyIconPath => "res://newsanguo/images/ui/energy/CaoWei/energy.png";

    public override string? TextEnergyIconPath => "res://newsanguo/images/ui/energy/CaoWei/energy_small.png";

    public override bool IsColorless => false;

    // 卡牌边框材质：在这里直接构造（原版 hsv.gdshader + 本 mod 的色相/饱和/明度）。
    //
    // 历史：2026-09-23 这里曾被改成加载
    // "res://materials/cards/frames/card_frame_newsanguo_mat.tres"（该路径此前写错成 res://newsanguo/…，
    // 永远加载失败，一直由下面这段构造代码顶着）。虽然 .tres 里声明的 h/s/v 与代码完全相同，
    // 但改用 .tres 后实际观感出现了可辨的偏色（用户反馈），因此改回"只认代码构造"这一条路径。
    // 同一份数据仍保留在 res://materials/cards/frames/card_frame_newsanguo_mat.tres 作为记录，代码不再引用它；
    // RitsuLib 的 CardFrameMaterialPath 回退也因此不会被用到（PoolFrameMaterial 恒为非 null）。
    private static readonly Lazy<ShaderMaterial> _frameMaterial = new(() =>
    {
        Shader? shader = GD.Load<Shader>("res://shaders/hsv.gdshader");
        ShaderMaterial material = new()
        {
            Shader = shader,
            ResourceLocalToScene = true
        };
        // 将指定的 RGB 底色转换为 HSV，统一卡框材质与牌组列表底色。
        Color baseColor = new("244C73");
        material.SetShaderParameter("h", baseColor.H);
        material.SetShaderParameter("s", baseColor.S);
        material.SetShaderParameter("v", baseColor.V);
        return material;
    });

    public override Material? PoolFrameMaterial => _frameMaterial.Value;

    [Obsolete("基类要求保留，请使用新的起始牌注册方式。")]
    protected override IEnumerable<Type> CardTypes =>
    [
        // 2026-10-05：原 StrikeNewsanguo/DefendNewsanguo 拆成按角色各自一张
        // （曹魏用 StrikeCaowei/DefendCaowei、蜀汉用 StrikeShuhan/DefendShuhan），
        // 这样基础牌的牌框颜色由各自角色卡池决定。
        typeof(StrikeCaowei),
        typeof(DefendCaowei),
        typeof(AGrandToast),
        typeof(CrossTheRiverToo),
        typeof(FeelNoAcid),
        typeof(MaClanQuadBlast),
        typeof(SlamTheBowl),
        typeof(ToABiggerGoblet),
        typeof(BladesOfVirtue),
        typeof(WineIsTheOldHero),
        typeof(StarryNight),
        typeof(ScorchingStarfall),
        typeof(Divination),
        typeof(VictoryByHeavensWill),
        typeof(Plot),
        typeof(MindControlSpell),
        typeof(PeekIntoHeaven),
        typeof(DesecrateHeaven),
        typeof(SmilingTiger),
        typeof(DarkHornShark),
        typeof(HumanTransmutationSpell),
        typeof(ReanimationSpell),
        typeof(LongevitySpell),
        typeof(FatherCanClaimTheThrone),
        typeof(NewGamePlus),
        typeof(BrewLimitBreak),
        typeof(NearAndFar),
        typeof(Onset),
        typeof(TheTruestMask),
        typeof(DivineInsight),
        typeof(SeaChange),
        typeof(ThrowHimOut),
        typeof(HeavenRevision),
        typeof(MedicalMastery),
        typeof(Tweak),
        typeof(Release),
        // 2026-10-09：「自刎归天！」（FallOnOwnSword）按要求移出本池（改注册到蜀汉卡池）
        // 2026-10-05：「天下无敌」（Invincible）按要求移出本池（改注册到蜀汉卡池）
        // 2026-10-05：新增「参见汉中王！」
        typeof(HailKingOfHanzhong),
        typeof(CricketForm),
        typeof(TriumphBrew),
        typeof(WhatToEat),
        typeof(DongZhuoTheTraitor),
        typeof(HeavenHatingSwordplay),
        typeof(RuthlessBlade),
        typeof(BoneMeltingPalm),
        typeof(WolfVsDog),
        typeof(FireAndWaterProof),
        typeof(CentralPlainsPass),
        typeof(ProxyStrike),
        typeof(Tremble),
        typeof(BetterThanYilingFlames),
        typeof(JustKidding),
        typeof(BetterEachDay),
        typeof(Retire),
        typeof(BrewHealsAll),
        typeof(CheckThePremiere),
        // 2026-10-08：按要求把「天上人间」从曹魏卡池移除（改为只注册在蜀汉卡池）
        typeof(WorthAllTheirLives),
        typeof(DragonOmen),
        typeof(OffWithYourHead),

        // 2026-10-08 追加：曹魏新卡「天上大水」（只在本池注册，蜀汉拿不到）
        typeof(HeavenlyDeluge),             // 天上大水

        // 2026-10-08 追加：曹魏新卡「时光酸雨」（原名「时空酸雨」，2026-10-09 改名；只在本池注册，蜀汉拿不到）
        typeof(TimeAcidRain),               // 时光酸雨

        // 2026-10-09 追加：曹魏新卡「直奔诸葛亮四轮车！」（只在本池注册，蜀汉拿不到；
        // 其衍生牌「四轮车」注册在衍生池 TokenCardPool，不进本池）
        typeof(ChargeToZhugeLiangsCart)     // 直奔诸葛亮四轮车！
    ];
}
