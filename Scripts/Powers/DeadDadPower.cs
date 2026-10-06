using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace newsanguo.Scripts.Powers;

/// <summary>
/// “迭死亡”（Dead Dad）：每当你打出一张牌，就在本回合内获得与层数相等的[敏捷]。
///
/// 挂在谁身上就按谁的视角生效：
///  · 挂在玩家身上 —— 只统计该玩家自己打出的牌，他自己获得敏捷；
///  · 挂在怪物（盛碗虫（巨石））身上 —— 怪物不会“打牌”，所以这里把“打出牌的人”当作获得方：
///    哪名玩家打出牌，那名玩家就在本回合内获得敏捷（多人模式下各算各的账）。
///
/// 「本回合内」的写法：每次打牌当场给真实敏捷，回合结束时把自己这一回合给出去的敏捷一次收回。
/// 为什么不用原版 TemporaryDexterityPower：它是抽象类，且 OriginModel 只接受卡/药水/遗物
/// （原版 TemporaryDexterityPower.Title / ExtraHoverTips 遇到别的模型类型会抛 InvalidOperationException），
/// 本能力是直接施加到生物身上的、没有对应的卡模型，所以这里自己记账（见 Data）。
/// 账本按“获得方”分开记，因此层数被改大改小、或者别的效果也加敏捷，都不会算错；
/// 多人模式下每名玩家的账各自在他自己的回合结束时结算。
///
/// 显示名/描述在 localization/*/powers.json 的 NEWSANGUO_POWER_DEAD_DAD_POWER.* 里；
/// 图标是 newsanguo/images/powers/DeadDadPower.png 与 DeadDadPowerBig.png。
/// </summary>
[RegisterPower]
public class DeadDadPower : ModPowerTemplate
{
    // 账本：每个获得过本能力给的敏捷的生物各记一笔（宿主回合结束时按各自的账扣回）
    private class Data
    {
        public readonly Dictionary<Creature, int> GrantedThisTurn = [];
    }

    // 正面效果
    public override PowerType Type => PowerType.Buff;
    // 计数器：层数 = 每打出一张牌获得的敏捷（多次施加叠加）
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不允许负数
    public override bool AllowNegative => false;
    // 出牌/回合结束钩子需要战斗上下文
    public override bool ShouldReceiveCombatHooks => true;

    // 能力图标资源（newsanguo/images/powers/DeadDadPower.png 与 DeadDadPowerBig.png）
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://newsanguo/images/powers/DeadDadPower.png",
        BigIconPath: "res://newsanguo/images/powers/DeadDadPowerBig.png"
    );

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 每打出一张牌：打出牌的那一方（挂在玩家身上时就是宿主自己）本回合 +Amount 点敏捷
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is null || !Owner.IsAlive || Amount <= 0)
        {
            return;
        }

        Creature? player = cardPlay.Card?.Owner?.Creature;
        if (player is null || !player.IsAlive)
        {
            return;
        }

        Creature grantee;
        if (Owner.IsPlayer)
        {
            // 挂在玩家身上：只算宿主自己打出的牌（多人模式下忽略其他玩家）
            if (player != Owner)
            {
                return;
            }

            grantee = Owner;
        }
        else
        {
            // 挂在怪物身上：谁打牌谁获得
            grantee = player;
        }

        Flash();
        Data data = GetInternalData<Data>();
        data.GrantedThisTurn.TryGetValue(grantee, out int alreadyGranted);
        data.GrantedThisTurn[grantee] = alreadyGranted + Amount;
        await PowerCmd.Apply<DexterityPower>(choiceContext, grantee, Amount, Owner, cardPlay.Card);
    }

    // 玩家方回合结束：把这一回合给出去的敏捷按人收回（只结算刚刚结束的这一方）
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        Data data = GetInternalData<Data>();
        if (data.GrantedThisTurn.Count == 0 || side != CombatSide.Player)
        {
            return;
        }

        // 先收集要结算的对象，避免遍历时改字典
        List<Creature> settled = [];
        foreach (KeyValuePair<Creature, int> entry in data.GrantedThisTurn)
        {
            if (entry.Value > 0 && participants.Contains(entry.Key))
            {
                settled.Add(entry.Key);
            }
        }

        foreach (Creature creature in settled)
        {
            int granted = data.GrantedThisTurn[creature];
            data.GrantedThisTurn.Remove(creature);
            Flash();
            await PowerCmd.Apply<DexterityPower>(choiceContext, creature, -granted, Owner, null);
        }
    }
}
