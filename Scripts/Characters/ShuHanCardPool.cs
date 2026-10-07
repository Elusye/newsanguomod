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
public class ShuHanCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    public override string Title => "newsanguo";

    public override string EnergyColorName => "newsanguo";

    public override string CardFrameMaterialPath => "card_frame_newsanguo";

    public override Color DeckEntryCardColor => new Color("1E3B2E");

    public override Color EnergyOutlineColor => new Color("12261C");

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
        material.SetShaderParameter("h", 0.36f);
        material.SetShaderParameter("s", 0.55f);
        material.SetShaderParameter("v", 0.42f);
        return material;
    });

    public override Material? PoolFrameMaterial => _frameMaterial.Value;

    [Obsolete("基类要求保留，请使用新的起始牌注册方式。")]
    protected override IEnumerable<Type> CardTypes =>
    [
        // 2026-10-05：原 StrikeNewsanguo/DefendNewsanguo 拆成按角色各自一张
        // （曹魏用 StrikeCaowei/DefendCaowei、蜀汉用 StrikeShuhan/DefendShuhan），
        // 蜀汉这两个只在本池注册，牌框因此跟随蜀汉的墨绿配色。
        typeof(StrikeShuhan),
        typeof(DefendShuhan),
        typeof(AGrandToast),
        typeof(BrewHealsAll),
        typeof(BrewLimitBreak),
        typeof(ImGettingDrunk),
        typeof(Intoxicated),
        typeof(LoathToLeaveTheTable),
        typeof(NewGamePlus),
        typeof(ScorchingStarfall),
        typeof(ThreeBlades),
        typeof(ToABiggerGoblet),
        typeof(TriumphBrew),
        typeof(WhereSWine),
        typeof(WineCut),
        typeof(WineTheOldHero),

        // 2026-10-04 追加：这些牌原本只在新三国（曹魏）卡池里，现同时加入蜀汉卡池
        typeof(BladeOfVirtue),              // 仁之剑，义之剑
        typeof(RuthlessBlade),              // 无情剑法
        typeof(DragonOmen),                 // 龙可是帝王之征啊
        typeof(DeafenMe),                   // 扎聋我自己的耳朵！
        typeof(QuadBlast),                  // 马氏四连
        typeof(ProxyStrike),                // 替身打击
        typeof(OffWithYourHead),            // 我砍你的头！
        typeof(PartyOn),                    // 接着奏乐接着舞
        typeof(WhyPickThatUp),              // 你拾它做甚！
        typeof(CrossForCross),              // 他过江我也过江！
        typeof(WindOfTiger),                // 风从虎，云从龙
        typeof(SmilingTiger),               // 笑面虎
        typeof(DarkfinShark),               // 乌角鲨
        typeof(Release),                    // 释怀
        typeof(BetterThanYilingFlames),     // 比夷陵之火还好啊
        typeof(FortySixtyTax),              // 四六征税
        typeof(TenThousandTransparentHoles),// 一万个透明窟窿！
        typeof(SelfFall),                   // 自刎归天！
        typeof(HumanTransmutationSpell),    // 人体炼成术

        // 2026-10-05 追加：按要求把「天上人间」「天下无敌」加入蜀汉卡池
        typeof(HeavenAndEarth),             // 天上人间
        typeof(Invincible),                 // 天下无敌！（已从曹魏卡池移除）

        // 2026-10-05 追加：蜀汉新卡「人肉打印机」（只在本池注册，曹魏拿不到）
        typeof(HumanPrinter),               // 人肉打印机

        // 2026-10-05 追加：蜀汉新卡「帅台设宴」（只在本池注册，曹魏拿不到）
        typeof(MarshalsTerraceFeast),       // 帅台设宴

        // 2026-10-05 追加：蜀汉新卡「博望坡悖论」（只在本池注册，曹魏拿不到）
        typeof(BowangSlopeParadox),         // 博望坡悖论

        // 2026-10-06 追加：蜀汉先古卡「创造模式」（只在本池注册，曹魏拿不到）
        typeof(CreativeMode),               // 创造模式

        // 2026-10-07 追加：蜀汉新卡「狂妄之人」（只在本池注册，曹魏拿不到）
        typeof(Megalovania),                // 狂妄之人

        // 2026-10-07 追加：蜀汉新卡「托管」（只在本池注册，曹魏拿不到）
        typeof(AutoPilot),                  // 托管

        // 2026-10-07 追加：蜀汉新卡「君王形态」（原名「真正的君王」/ The True King，只在本池注册，曹魏拿不到）
        typeof(SovereignForm)               // 君王形态
    ];
}