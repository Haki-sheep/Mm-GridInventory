using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 跨容器放置协调器
    /// 操作两个 InventoryState 完成放置 堆叠 交换
    /// </summary>
    public static class InventoryCrossContainerService
    {
        /// <summary>
        /// 跨容器放置入口
        /// 落点容器先收 A 源容器再接回 B 或 displaced 小物
        /// 任一步失败则双快照还原
        /// </summary>
        /// <param name="sourceState">源背包状态</param>
        /// <param name="targetState">落点背包状态</param>
        /// <param name="dragItem">被拖拽物</param>
        /// <param name="sourceAnchor">A 侧拖起锚点</param>
        /// <param name="dropAnchor">B 侧预览落点</param>
        /// <returns>跨容器操作结果</returns>
        public static CrossContainerOpResult TryCrossContainerDrop(InventoryState sourceState,
                                                                   InventoryState targetState,
                                                                   IItemRuntime dragItem,
                                                                   Vector2Int sourceAnchor,
                                                                   Vector2Int dropAnchor)
        {
            if (dragItem is null || sourceState is null || targetState is null)
                return CrossContainerOpResult.Fail(null);

            // 两侧各拍快照 B 成功 A 失败时用来整体回滚
            var sourceSnapshot = sourceState.CaptureSnapshot();
            var targetSnapshot = targetState.CaptureSnapshot();

            // 第一步 只改 B 网格 拖入物进落点容器
            var targetResult = TryReceiveOnTarget(targetState, dragItem, dropAnchor);
            if (!targetResult.IsSuccess)
                return targetResult;

            // 第二步 只改 A 网格 把 B 换出的物接回源容器
            if (TryReceiveReturnOnSource(sourceState, targetResult, sourceAnchor, dropAnchor))
                return targetResult;

            // A 接回失败 B 侧已经改过 必须双快照还原到落点前
            sourceState.RestoreSnapshot(sourceSnapshot);
            targetState.RestoreSnapshot(targetSnapshot);
            return CrossContainerOpResult.Fail(dragItem, targetResult.ItemDataB, targetResult.SwapState);
        }

        /// <summary>
        /// 落点容器接收拖入物
        /// 优先级 直接放置 堆叠 CrossContainer 半交换
        /// </summary>
        private static CrossContainerOpResult TryReceiveOnTarget(InventoryState targetState,
                                                                 IItemRuntime dragItem,
                                                                 Vector2Int dropAnchor)
        {
            // 预览格为空 拖入物直接进 B
            if (targetState.CanPlace(dragItem, dropAnchor))
            {
                if (!targetState.SetAt(dropAnchor, dragItem))
                    return CrossContainerOpResult.Fail(dragItem);

                return new CrossContainerOpResult(true, dragItem);
            }

            var itemAtDrop = targetState.GetItemByMask(dropAnchor);

            // 落点有同类可堆叠物 合并后 A 可能被消耗为 null
            if (itemAtDrop is not null
                && targetState.CanStack(dragItem, itemAtDrop)
                && targetState.TryStack(dragItem, itemAtDrop))
            {
                return new CrossContainerOpResult(true, null, itemAtDrop);
            }

            // 落点被占且不能堆叠 尝试与目标物交换
            if (!targetState.TryGetSwapTargetItem(dragItem, dropAnchor, out var swapTargetItem))
                return CrossContainerOpResult.Fail(dragItem);

            var swapDisplacedList = new List<IItemRuntime>();
            var eSwapState = targetState.GetSwapState(dragItem, swapTargetItem);

            // CrossContainer 模式只执行 B 侧半交换 换出的 B 留给源容器接回
            if (targetState.TrySwap(dragItem,
                                    swapTargetItem,
                                    swapDisplacedList,
                                    dropAnchor,
                                    ESwapPlaceMode.CrossContainer))
            {
                return new CrossContainerOpResult(true,
                                                  dragItem,
                                                  swapTargetItem,
                                                  swapDisplacedList,
                                                  eSwapState);
            }

            // TrySwap 内部失败会自回滚 B 网格
            return CrossContainerOpResult.Fail(dragItem, swapTargetItem, eSwapState);
        }

        /// <summary>
        /// 源容器接收 B 侧换回的物品
        /// </summary>
        private static bool TryReceiveReturnOnSource(InventoryState sourceState,
                                                     CrossContainerOpResult targetResult,
                                                     Vector2Int sourceAnchor,
                                                     Vector2Int dropAnchor)
        {
            switch (targetResult.SwapState)
            {
                // 等量交换或小换大 单个 B 回到 A 拖起锚点
                case ESwapState.Same:
                case ESwapState.SmallToLarge:
                    if (targetResult.ItemDataB is null)
                        return false;

                    // A 拖起处已在 BeginDrag 腾空 一般可直接放 B
                    if (sourceState.SetAt(sourceAnchor, targetResult.ItemDataB))
                        return true;

                    // 小换大时 B 可能比 A 原 footprint 大 原锚点放不下则全背包找空位
                    return targetResult.SwapState == ESwapState.SmallToLarge
                           && sourceState.SetAtFirst(targetResult.ItemDataB, out _);

                // 大换小 多个被挤小物按相对布局映射回 A
                case ESwapState.LargeToSmall:
                    return TryReceiveDisplacedList(sourceState,
                                                   targetResult.DisplacedItemDataList,
                                                   dropAnchor,
                                                   sourceAnchor);

                // 直接放置或堆叠 无需 A 侧接回
                default:
                    return true;
            }
        }

        /// <summary>
        /// 源容器按相对偏移接收大换小被挤开的小物品
        /// </summary>
        private static bool TryReceiveDisplacedList(InventoryState sourceState,
                                                    List<IItemRuntime> displacedItemDataList,
                                                    Vector2Int dropAnchor,
                                                    Vector2Int sourceAnchor)
        {
            if (displacedItemDataList is null || displacedItemDataList.Count == 0)
                return true;

            for (int i = 0; i < displacedItemDataList.Count; i++)
            {
                var itemData = displacedItemDataList[i];
                if (itemData is null)
                    return false;

                // 保持小物相对大物落点的摆放关系 映射到 A 拖起锚点
                // 例 B 落点 (2,1) 小物在 (3,1) 偏移 (1,0) → A 拖起 (0,0) 则放到 (1,0)
                var relativeOffset = itemData.AnchorPos - dropAnchor;
                if (!sourceState.SetAt(sourceAnchor + relativeOffset, itemData))
                    return false;
            }

            return true;
        }
    }
}
