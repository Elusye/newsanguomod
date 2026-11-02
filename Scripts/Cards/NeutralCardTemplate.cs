using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using newsanguo.Scripts.Characters;

namespace newsanguo.Scripts.Cards;

/// <summary>双方共用同一牌 ID；逻辑来源和卡面始终属于棕色中立池。</summary>
public abstract class NeutralCardTemplate : NewsanguoCardTemplate
{
    public override CardPoolModel Pool => ModelDb.CardPool<NeutralCardPool>();

    protected NeutralCardTemplate(int cost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true) : base(cost, type, rarity, target, showInCardLibrary) { }
}
