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

namespace newsanguo.Scripts.Characters;

[RegisterSharedCardPool]
public class NewsanguoCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    public override string Title => "newsanguo";

    public override string EnergyColorName => "newsanguo";

    public override string CardFrameMaterialPath => "card_frame_newsanguo";

    public override Color DeckEntryCardColor => new Color("6B492E");

    public override Color EnergyOutlineColor => new Color("4A2F1C");

    // 能量图标路径（RitsuLib 官方覆盖）：
    // - BigEnergyIconPath：EnergyIconHelper.GetPath 的大图标（卡面/遗物/药水费用图标等）
    // - TextEnergyIconPath：卡牌描述 {Energy:energyIcons()} 富文本图标（24x24 小图，避免 128x128 源图渲染/测量失真）
    public override string? BigEnergyIconPath => "res://newsanguo/images/ui/energy_newsanguo.png";

    public override string? TextEnergyIconPath => "res://newsanguo/images/ui/energy_newsanguo_small.png";

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
        material.SetShaderParameter("h", 0.07f);
        material.SetShaderParameter("s", 0.7f);
        material.SetShaderParameter("v", 0.8f);
        return material;
    });

    public override Material? PoolFrameMaterial => _frameMaterial.Value;

    [Obsolete("基类要求保留，请使用新的起始牌注册方式。")]
    protected override IEnumerable<Type> CardTypes =>
    [
        typeof(StrikeNewsanguo),
        typeof(DefendNewsanguo),
        typeof(AGrandToast),
        typeof(CrossForCross),
        typeof(FeelNoAcid),
        typeof(QuadBlast),
        typeof(SlamTheBowl),
        typeof(ToABiggerGoblet),
        typeof(BladeOfVirtue),
        typeof(WineTheOldHero),
        typeof(StarryNight),
        typeof(ScorchingStarfall),
        typeof(Divination),
        typeof(VictoryByHeavensWill),
        typeof(Plot),
        typeof(MindControlSpell),
        typeof(PeekIntoHeaven),
        typeof(DesecrateHeaven),
        typeof(SmilingTiger),
        typeof(DarkfinShark),
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
        typeof(GetOut),
        typeof(HeavenRevision),
        typeof(MedicalMastery),
        typeof(Tweak),
        typeof(SelfFall),
        typeof(Release),
        typeof(Invincible),
        typeof(CricketForm),
        typeof(TriumphBrew),
        typeof(WhatToEat),
        typeof(DongZhuoTheTraitor),
        typeof(SkywardBlade),
        typeof(RuthlessBlade),
        typeof(BonelessPalm),
        typeof(WolfVsDog),
        typeof(Unstoppable),
        typeof(CentralBastion),
        typeof(ProxyStrike),
        typeof(Tremble),
        typeof(BetterThanYilingFlames),
        typeof(JustKidding),
        typeof(BetterEachDay),
        typeof(Retire),
        typeof(BrewHealsAll),
        typeof(CheckThePremiere),
        typeof(HeavenAndEarth),
        typeof(DeafenMe),
        typeof(DragonOmen),
        typeof(OffWithYourHead)
    ];
}
