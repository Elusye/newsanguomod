using System;
using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Characters;

[RegisterSharedCardPool]
// 不加入 Colorful Philosophers 事件的可交换颜色池。
public class ShuHanCardPool : TypeListCardPoolModel
{
    public override string Title => "newsanguo";

    public override string EnergyColorName => "newsanguo_shuhan";

    public override string CardFrameMaterialPath => "card_frame_newsanguo";

    public override Color DeckEntryCardColor => new("8B0000");

    public override Color EnergyOutlineColor => new("12261C");

    // 能量图标路径（RitsuLib 官方覆盖）：
    // - BigEnergyIconPath：EnergyIconHelper.GetPath 的大图标（卡面/遗物/药水费用图标等）
    // - TextEnergyIconPath：卡牌描述 {Energy:energyIcons()} 富文本图标（24x24 小图，避免 128x128 源图渲染/测量失真）
    public override string? BigEnergyIconPath => "res://newsanguo/images/ui/energy/ShuHan/energy.png";

    public override string? TextEnergyIconPath => "res://newsanguo/images/ui/energy/ShuHan/energy_small.png";

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
        Color baseColor = new("8B0000");
        material.SetShaderParameter("h", baseColor.H);
        material.SetShaderParameter("s", baseColor.S);
        material.SetShaderParameter("v", baseColor.V);
        return material;
    });

    public override Material? PoolFrameMaterial => _frameMaterial.Value;

    // 卡牌全部通过各自类上的 [RegisterCard] 逐张注入；共有玩法的牌使用独立的 ShuHan 实体，
    // 因而卡池来源会随卡牌 ID 一起保留下来。
}
