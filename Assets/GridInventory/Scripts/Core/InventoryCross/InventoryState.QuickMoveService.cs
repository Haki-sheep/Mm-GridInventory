using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public partial class InventoryState
    {
        /// <summary> 快捷互转服务 绑定源背包 </summary>
        private readonly InventoryQuickMoveService quickMoveService;

        /// <summary>
        /// 从本背包快捷移动到目标背包
        /// 先堆叠 再 SetAtFirstWithRotate 整件放入
        /// </summary>
        /// <param name="targetState">目标背包状态</param>
        /// <param name="item">被移动物品</param>
        /// <returns>快捷互转结果</returns>
        public QuickMoveOpResult TryQuickMoveTo(InventoryState targetState, IItemRuntime item) =>
            quickMoveService.TryMoveTo(targetState, item);

        /// <summary>
        /// 快捷互转服务
        /// </summary>
        private sealed class InventoryQuickMoveService
        {
            /// <summary> 源背包状态 </summary>
            private readonly InventoryState sourceState;

            /// <summary> 扫描锚点物品临时列表 </summary>
            private readonly List<IItemRuntime> tempAnchorItemList = new();

            /// <summary>
            /// 初始化快捷互转服务
            /// </summary>
            /// <param name="sourceState">源背包状态</param>
            public InventoryQuickMoveService(InventoryState sourceState)
            {
                this.sourceState = sourceState;
            }

            /// <summary>
            /// 快捷移动到目标背包
            /// </summary>
            public QuickMoveOpResult TryMoveTo(InventoryState targetState, IItemRuntime item)
            {
                if (item is null || targetState is null)
                    return QuickMoveOpResult.Fail();

                var sourceAnchor = item.AnchorPos;

                // 先在目标背包找可堆叠物
                if (TryStackOnTarget(targetState, item, out var stackTarget))
                {
                    if (item.CurrStackCount <= 0)
                        sourceState.RemoveAt(sourceAnchor);

                    var eKind = item.CurrStackCount <= 0
                        ? EQuickMoveResultKind.StackedFull
                        : EQuickMoveResultKind.StackedPartial;
                    return new QuickMoveOpResult(eKind, item, stackTarget);
                }

                // 整件移动 先从源背包取下
                if (!sourceState.RemoveAt(sourceAnchor))
                    return QuickMoveOpResult.Fail();

                // 目标背包找空位 朝向优先 必要时旋转 逻辑在 PlacementService
                if (targetState.SetAtFirstWithRotate(item, out _))
                    return new QuickMoveOpResult(EQuickMoveResultKind.Moved, item);

                // 失败 放回源背包 朝向已由 SetAtFirstWithRotate 还原
                if (!sourceState.SetAt(sourceAnchor, item))
                    Debug.LogError("快捷互转回滚失败 源背包无法还原物品");

                return QuickMoveOpResult.Fail();
            }

            /// <summary>
            /// 在目标背包扫描可堆叠目标并合并
            /// </summary>
            private bool TryStackOnTarget(InventoryState targetState,
                                          IItemRuntime item,
                                          out IItemRuntime stackTarget)
            {
                stackTarget = null;
                targetState.CollectAnchorItems(tempAnchorItemList);

                for (int i = 0; i < tempAnchorItemList.Count; i++)
                {
                    var candidate = tempAnchorItemList[i];
                    if (!targetState.CanStack(item, candidate))
                        continue;

                    if (!targetState.TryStack(item, candidate))
                        continue;

                    stackTarget = candidate;
                    return true;
                }

                return false;
            }
        }
    }
}
