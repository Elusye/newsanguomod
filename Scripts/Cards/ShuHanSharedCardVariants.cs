using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

/// <summary>
/// 共有玩法牌的蜀汉实体。
/// 每个类型只注册进蜀汉池；原类型只注册进曹魏池，因此卡牌离开原角色后仍能从自身 ID
/// 找回正确的卡池、卡框材质和能量图标。玩法实现继承原类型，卡图也复用原图。
/// </summary>
internal static class ShuHanSharedCardAssets
{
    internal static CardAssetProfile ReusePortraitOf<TCard>() => new(
        PortraitPath: $"res://newsanguo/images/cards/{typeof(TCard).Name}.png");
}

[RegisterCard(typeof(ShuHanCardPool))]
[RegisterCharacterStarterCard(typeof(ShuHanCharacter), 1)]
public sealed class AGrandToastShuHan : AGrandToast
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<AGrandToast>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class BetterThanYilingFlamesShuHan : BetterThanYilingFlames
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<BetterThanYilingFlames>();
}

[RegisterCard(typeof(ShuHanCardPool))]
[RegisterCharacterStarterCard(typeof(ShuHanCharacter), 1)]
public sealed class BladesOfVirtueShuHan : BladesOfVirtue
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<BladesOfVirtue>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class BrewHealsAllShuHan : BrewHealsAll
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<BrewHealsAll>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class BrewLimitBreakShuHan : BrewLimitBreak
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<BrewLimitBreak>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class CrossTheRiverTooShuHan : CrossTheRiverToo
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<CrossTheRiverToo>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class DarkHornSharkShuHan : DarkHornShark
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<DarkHornShark>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class DragonOmenShuHan : DragonOmen
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<DragonOmen>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class FortySixtyTaxShuHan : FortySixtyTax
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<FortySixtyTax>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class HumanTransmutationSpellShuHan : HumanTransmutationSpell
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<HumanTransmutationSpell>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class ImGettingDrunkShuHan : ImGettingDrunk
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<ImGettingDrunk>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class IntoxicatedShuHan : Intoxicated
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<Intoxicated>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class MaClanQuadBlastShuHan : MaClanQuadBlast
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<MaClanQuadBlast>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class NewGamePlusShuHan : NewGamePlus
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<NewGamePlus>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class OffWithYourHeadShuHan : OffWithYourHead
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<OffWithYourHead>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class PartyOnShuHan : PartyOn
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<PartyOn>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class ProxyStrikeShuHan : ProxyStrike
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<ProxyStrike>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class ReleaseShuHan : Release
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<Release>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class RuthlessBladeShuHan : RuthlessBlade
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<RuthlessBlade>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class ScorchingStarfallShuHan : ScorchingStarfall
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<ScorchingStarfall>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class SmilingTigerShuHan : SmilingTiger
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<SmilingTiger>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class ThreeBladesShuHan : ThreeBlades
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<ThreeBlades>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class TigerWindCloudDragonShuHan : TigerWindCloudDragon
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<TigerWindCloudDragon>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class ToABiggerGobletShuHan : ToABiggerGoblet
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<ToABiggerGoblet>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class TriumphBrewShuHan : TriumphBrew
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<TriumphBrew>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class WheresWineShuHan : WheresWine
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<WheresWine>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class WineCutShuHan : WineCut
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<WineCut>();
}

[RegisterCard(typeof(ShuHanCardPool))]
public sealed class WineIsTheOldHeroShuHan : WineIsTheOldHero
{
    public override CardAssetProfile AssetProfile => ShuHanSharedCardAssets.ReusePortraitOf<WineIsTheOldHero>();
}
