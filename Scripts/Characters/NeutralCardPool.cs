using System;
using Godot;
using MegaCrit.Sts2.Core.Unlocks;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Characters;

[RegisterSharedCardPool]
public class NeutralCardPool : TypeListCardPoolModel
{
    // 每张牌只有一个自动注册特性，双方奖励池通过显式 API 注入。
    internal static void RegisterSharedCards()
    {
        var content = STS2RitsuLib.Content.ModContentRegistry.For(Entry.ModId);
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.SecretStratagem>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.SecretStratagem>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.ThrowHimOut>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.ThrowHimOut>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.StarryNight>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.StarryNight>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.CheckThePremiere>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.CheckThePremiere>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.ThreeBlades>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.ThreeBlades>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.QuakeBeforeMe>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.QuakeBeforeMe>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.Retire>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.Retire>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.Release>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.Release>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.NewGamePlus>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.NewGamePlus>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.GoldenRebellion>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.GoldenRebellion>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.DarkHornShark>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.DarkHornShark>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.ChainStratagem>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.ChainStratagem>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.SmilingTiger>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.SmilingTiger>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.WhatToEat>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.WhatToEat>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.WellFedWellClothed>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.WellFedWellClothed>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.FeelNoAcid>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.FeelNoAcid>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.DareYouToSlash>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.DareYouToSlash>();
        content.RegisterCard<NewsanguoCardPool, newsanguo.Scripts.Cards.SoImpressive>();
        content.RegisterCard<ShuHanCardPool, newsanguo.Scripts.Cards.SoImpressive>();
    }

    public override string Title => "newsanguo";

    public override string EnergyColorName => "newsanguo_caowei";

    public override string CardFrameMaterialPath => "card_frame_newsanguo";

    public override Color DeckEntryCardColor => new("CC793D");

    public override Color EnergyOutlineColor => new("4A2F1C");

    // 能量图标路径（RitsuLib 官方覆盖）：
    // - BigEnergyIconPath：EnergyIconHelper.GetPath 的大图标（卡面/遗物/药水费用图标等）
    // - TextEnergyIconPath：卡牌描述 {Energy:energyIcons()} 富文本图标（24x24 小图，避免 128x128 源图渲染/测量失真）
    public override string? BigEnergyIconPath => "res://newsanguo/images/ui/energy/Neutral/energy.png";

    public override string? TextEnergyIconPath => "res://newsanguo/images/ui/energy/Neutral/energy_small.png";

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
        Color baseColor = new("CC793D");
        material.SetShaderParameter("h", baseColor.H);
        material.SetShaderParameter("s", baseColor.S);
        material.SetShaderParameter("v", baseColor.V);
        return material;
    });

    public override Material? PoolFrameMaterial => _frameMaterial.Value;

    // 卡牌全部通过各自类上的 [RegisterCard] 逐张注入；不要再覆盖旧式 CardTypes，
    // 否则 TypeListCardPoolModel 会和 RitsuLib 的注入各生成一份相同模型。
}
