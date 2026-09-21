using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace newsanguo.Scripts.Patches;

// 兜底补丁：出牌期间等玩家选牌、恢复时要把那张卡的节点 reparent 回场上；
// 但如果这张卡是**能力牌（CardType.Power）**，它的节点早就被“飞向能力区”的动画回收掉了，
// 于是原版会连着报两个错，并把一张已回收的节点留在“玩家意图槽”上（多人下就是画面右下角）。
//
// 原版机制（可逐行核对）：
//   1) CardModel.OnPlayWrapper 对能力牌会先 await PlayPowerCardFlyVfx()（CardModel.cs:1907）；
//   2) 该动画由 NCardFlyPowerVfx 在后台跑（TaskHelper.RunSafely），结尾 CardNode.QueueFreeSafely()；
//   3) QueueFreeSafely 对池化节点（NCard : IPoolable）会**立刻把节点从父节点摘掉**、再延迟回收
//      （GodotTreeExtensions.cs:89）——此刻 GetParent() 已经是 null；
//   4) 能力牌的 OnPlay 若随后等玩家选牌，NMultiplayerPlayerIntentHandler.BeforeActionPausedForPlayerChoice
//      会把这个节点补间到玩家意图槽并记进 _cardInPlayAwaitingPlayerChoice（…:309-321）；
//   5) 恢复时 NCardPlayQueue.ReAddCardAfterPlayerChoice 再 reparent 就失败：
//        ERROR: Node needs a parent to be reparented.   (reparent)
//        ERROR: Child is not a child of this node.      (MoveChildSafely)
//      接着 BeforeRemoteCardPlayResumedAfterPlayerChoice 又失败一次。
//
// 本补丁只做一件事：**节点已经不在场景树里（reparent 注定失败）时跳过原版逻辑**。
// 这种“牌已经不在了”的情况本来就没什么可放回场上的；顺带因为不再走到订阅那一行，
// 后续那次 BeforeRemoteCardPlayResumedAfterPlayerChoice 的报错也一并消失。
//
// 为什么只有「赋值」中招：全游戏（原版 + 本 mod）只有它是“能力牌 + 出牌期间要玩家选牌”的卡。
// 原版最像的 DualWield 其实是技能牌（CardType.Power 只出现在它的选择过滤器里）。
[HarmonyPatch(typeof(NCardPlayQueue), nameof(NCardPlayQueue.ReAddCardAfterPlayerChoice))]
public static class ReAddCardAfterPlayerChoicePatch
{
    public static bool Prefix(NCard card)
    {
        return GodotObject.IsInstanceValid(card) && card.IsInsideTree();
    }
}
