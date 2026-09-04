using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

using newsanguo.Scripts;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “天降雄兵”：同一回合内打出的天降雄兵会叠加到同一实例（士兵数量累加），
/// 不同回合打出的各自独立倒计时。
/// 每个实例：Amount 表示累计士兵数量，倒计时固定为 2，经过 2 次玩家回合结束（弃牌后）
/// 时将 Amount 张“士兵”加入手牌并移除。
/// 图标数字：右下角由原版显示士兵数量（Amount），右上角通过 IHasSecondAmount 显示剩余回合数。
/// </summary>
[RegisterPower]
public class heavenly_troops_power : ModPowerTemplate, IHasSecondAmount
{
    // 初始倒计时：还需要经过的玩家回合结束次数
    private const int InitialTurnsLeft = 2;

    private class Data
    {
        // 剩余需要经过的玩家回合结束次数
        public int turnsLeft = InitialTurnsLeft;
        // 打出时的玩家回合号，用于判断同回合叠加
        public int turnNumber;
    }

    // 描述变量：剩余回合数（供 powers.json 描述中的 {TurnsLeft} 使用）
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new IntVar("TurnsLeft", InitialTurnsLeft)
    ];

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 计数器：右下角由原版直接显示士兵数量（Amount），右上角由 IHasSecondAmount 显示回合倒计时
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
    // 不同回合各自独立实例；同回合叠加由卡牌打出逻辑合并
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    // 回合结束钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"res://newsanguo/images/powers/{GetType().Name}.png",
        BigIconPath: $"res://newsanguo/images/powers/{GetType().Name}_big.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 记录打出时的回合号
    public void SetTurnNumber(int turnNumber)
    {
        GetInternalData<Data>().turnNumber = turnNumber;
    }

    // 判断是否与指定回合号相同（用于同回合叠加）
    public bool IsFromTurn(int turnNumber)
    {
        return GetInternalData<Data>().turnNumber == turnNumber;
    }

    // 再次打出时重置倒计时（士兵数量由 PowerCmd 累加，回合数回到 2）
    public void ResetTurnsLeft()
    {
        Data data = GetInternalData<Data>();
        data.turnsLeft = InitialTurnsLeft;
        SyncTurnsLeftDisplay();
    }

    // 同步剩余回合数到描述变量并刷新图标右上角数字
    private void SyncTurnsLeftDisplay()
    {
        int turnsLeft = GetInternalData<Data>().turnsLeft;
        if (DynamicVars.TryGetValue("TurnsLeft", out DynamicVar turnsLeftVar))
        {
            turnsLeftVar.BaseValue = turnsLeft;
        }
        this.InvokeSecondAmountChanged();
    }

    // 能力图标右上角第二数字：剩余回合数（右下角由原版显示士兵数量 Amount）
    public string GetSecondAmount()
    {
        return GetInternalData<Data>().turnsLeft.ToString();
    }

    // 玩家回合结束（弃牌后）时倒计时；归零时发放士兵并移除
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || !participants.Contains(Owner))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        data.turnsLeft--;
        if (data.turnsLeft > 0)
        {
            // 剩余回合数变化：刷新描述变量与图标角标
            SyncTurnsLeftDisplay();
            return;
        }

        await SpawnSoldiers(choiceContext);
        await PowerCmd.Remove(this);
    }

    // 生成 Amount 张“士兵”加入玩家手牌
    private async Task SpawnSoldiers(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Player is not Player player)
        {
            return;
        }

        ICombatState? combatState = CombatState;
        if (combatState is null)
        {
            return;
        }

        // 触发音效：士兵加入手牌
        NewsanguoSfx.Play("event:/newsanguo/sfx/heavenly_troops_power");

        for (int i = 0; i < Amount; i++)
        {
            CardModel soldierCard = combatState.CreateCard<soldier>(player);
            await CardPileCmd.AddGeneratedCardToCombat(soldierCard, PileType.Hand, player, CardPilePosition.Random);
        }
    }
}
