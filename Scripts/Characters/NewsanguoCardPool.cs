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

    private static readonly Lazy<ShaderMaterial> _frameMaterial = new(() =>
    {
        const string MaterialPath = "res://newsanguo/materials/cards/frames/card_frame_newsanguo_mat.tres";
        if (GodotResourcePath.TryLoad<Material>(MaterialPath, out Material? loaded) && loaded is ShaderMaterial shaderMat)
        {
            shaderMat.ResourceLocalToScene = true;
            return shaderMat;
        }

        Shader? shader = GD.Load<Shader>("res://shaders/hsv.gdshader");
        ShaderMaterial fallback = new()
        {
            Shader = shader,
            ResourceLocalToScene = true
        };
        fallback.SetShaderParameter("h", 0.07f);
        fallback.SetShaderParameter("s", 0.7f);
        fallback.SetShaderParameter("v", 0.8f);
        return fallback;
    });

    public override Material? PoolFrameMaterial => _frameMaterial.Value;

    [Obsolete("基类要求保留，请使用新的起始牌注册方式。")]
    protected override IEnumerable<Type> CardTypes =>
    [
        typeof(strike_newsanguo),
        typeof(defend_newsanguo),
        typeof(a_grand_toast),
        typeof(cross_for_cross),
        typeof(feel_no_acid),
        typeof(quad_blast),
        typeof(slam_the_bowl),
        typeof(to_a_bigger_goblet),
        typeof(blade_of_virtue),
        typeof(wine_the_old_hero),
        typeof(starry_night),
        typeof(scorching_starfall),
        typeof(divination),
        typeof(victory_by_heavens_will),
        typeof(plot),
        typeof(mind_control_spell),
        typeof(peek_into_heaven),
        typeof(desecrate_heaven),
        typeof(smiling_tiger),
        typeof(darkfin_shark),
        typeof(human_transmutation_spell),
        typeof(reanimation_spell),
        typeof(longevity_spell),
        typeof(father_can_claim_the_throne),
        typeof(new_game_plus),
        typeof(brew_limit_break),
        typeof(near_and_far),
        typeof(onset),
        typeof(the_truest_mask),
        typeof(divine_insight),
        typeof(sea_change),
        typeof(get_out),
        typeof(heaven_revision),
        typeof(medical_mastery),
        typeof(tweak),
        typeof(self_fall),
        typeof(release),
        typeof(invincible),
        typeof(cricket_form),
        typeof(triumph_brew),
        typeof(what_to_eat),
        typeof(dong_zhuo_the_traitor),
        typeof(skyward_blade),
        typeof(ruthless_blade),
        typeof(boneless_palm),
        typeof(wolf_vs_dog),
        typeof(unstoppable),
        typeof(central_bastion),
        typeof(proxy_strike),
        typeof(tremble),
        typeof(better_than_yiling_flames),
        typeof(just_kidding),
        typeof(better_each_day),
        typeof(retire),
        typeof(brew_heals_all),
        typeof(check_the_premiere),
        typeof(heaven_and_earth),
        typeof(deafen_me),
        typeof(dragon_omen),
        typeof(off_with_your_head)
    ];
}
