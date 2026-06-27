using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public partial class InventoryState
    {
        private sealed class InventorySwapService
        {
            /// <summary> 背包状态引用 </summary>
            private readonly InventoryState inventoryState;
            /// <summary> 临时覆盖物品集合 </summary>
            private readonly HashSet<IItemRuntime> tempLittleItemHashList = new();
            /// <summary>
            /// 临时小物品相对偏移字典
            /// </summary>
            private readonly Dictionary<IItemRuntime, Vector2Int> tempLittleItemOffsetDict = new();
            /// <summary> 试算用临时列表 </summary>
            private readonly List<IItemRuntime> simulateOldItemList = new();

            /// <summary>
            /// 初始化交换服务
            /// </summary>
            /// <param name="inventoryState">背包状态</param>
            public InventorySwapService(InventoryState inventoryState)
            {
                this.inventoryState = inventoryState;
            }

            #region 对外交换接口

            /// <summary>
            /// 检查是否可交换
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否可交换</returns>
            public bool CanSwap(IItemRuntime aItemData,
                                IItemRuntime bItemData,
                                Vector2Int placeAnchorPos,
                                ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer) =>
                SimulateSwap(aItemData, bItemData, placeAnchorPos, swapPlaceMode);

            /// <summary>
            /// 执行交换
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            public bool TrySwap(IItemRuntime aItemData,
                                IItemRuntime bItemData,
                                List<IItemRuntime> oldItemDataList,
                                Vector2Int placeAnchorPos,
                                ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer) =>
                CommitSwap(aItemData, bItemData, placeAnchorPos, oldItemDataList, swapPlaceMode);

            #endregion

            #region 提交与试算

            /// <summary>
            /// 在克隆网格上试算交换
            /// 不会修改真实背包数据
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否可以交换</returns>
            private bool SimulateSwap(IItemRuntime aItemData,
                                      IItemRuntime bItemData,
                                      Vector2Int placeAnchorPos,
                                      ESwapPlaceMode swapPlaceMode)
            {
                var plan = GetSwapPlan(aItemData, bItemData);
                if (plan.SwapState == ESwapState.CanNotSwap) return false;

                int aIndex = inventoryState.ToIndex(plan.aItemData.AnchorPos);
                int bIndex = inventoryState.ToIndex(plan.bItemData.AnchorPos);

                var simAnchorArray = (IItemRuntime[])inventoryState.itemAnchorArray.Clone();
                var simOccupancyArray = (IItemRuntime[])inventoryState.occupancyOwnerArray.Clone();
                var realAnchorArray = inventoryState.itemAnchorArray;
                var realOccupancyArray = inventoryState.occupancyOwnerArray;

                inventoryState.itemAnchorArray = simAnchorArray;
                inventoryState.occupancyOwnerArray = simOccupancyArray;
                try
                {
                    return TryExecuteSwap(plan,
                                          aIndex,
                                          bIndex,
                                          placeAnchorPos,
                                          simulateOldItemList,
                                          swapPlaceMode);
                }
                finally
                {
                    inventoryState.itemAnchorArray = realAnchorArray;
                    inventoryState.occupancyOwnerArray = realOccupancyArray;
                }
            }

            /// <summary>
            /// 提交交换 失败时回滚真实网格
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            private bool CommitSwap(IItemRuntime aItemData,
                                    IItemRuntime bItemData,
                                    Vector2Int placeAnchorPos,
                                    List<IItemRuntime> oldItemDataList,
                                    ESwapPlaceMode swapPlaceMode)
            {
                var plan = GetSwapPlan(aItemData, bItemData);
                if (plan.SwapState == ESwapState.CanNotSwap)
                    return false;

                int aIndex = inventoryState.ToIndex(plan.aItemData.AnchorPos);
                int bIndex = inventoryState.ToIndex(plan.bItemData.AnchorPos);

                var backupItemArray = (IItemRuntime[])inventoryState.itemAnchorArray.Clone();
                var backupOccupancyOwner = (IItemRuntime[])inventoryState.occupancyOwnerArray.Clone();

                bool shouldRollback = true;
                try
                {
                    var canSwap = TryExecuteSwap(plan,
                                                 aIndex,
                                                 bIndex,
                                                 placeAnchorPos,
                                                 oldItemDataList,
                                                 swapPlaceMode);
                    if (!canSwap) return false;

                    for (int i = 0; i < inventoryState.itemAnchorArray.Length; i++)
                    {
                        var item = inventoryState.itemAnchorArray[i];
                        if (item is null) continue;
                        item.SetAnchorPos(inventoryState.ToVector2Int(i));
                    }

                    shouldRollback = false;
                    return true;
                }
                finally
                {
                    if (shouldRollback)
                    {
                        inventoryState.itemAnchorArray = backupItemArray;
                        inventoryState.occupancyOwnerArray = backupOccupancyOwner;
                    }
                }
            }

            /// <summary>
            /// 在当前网格上执行交换分支
            /// </summary>
            private bool TryExecuteSwap(SwapPlan plan,
                                        int aIndex,
                                        int bIndex,
                                        Vector2Int placeAnchorPos,
                                        List<IItemRuntime> oldItemDataList,
                                        ESwapPlaceMode swapPlaceMode)
            {
                oldItemDataList.Clear();
                switch (plan.SwapState)
                {
                    case ESwapState.Same:
                        return SwapSameItem(plan,
                                            aIndex,
                                            bIndex,
                                            placeAnchorPos,
                                            swapPlaceMode);

                    case ESwapState.LargeToSmall:
                        return SwapLargeToSmallItem(plan,
                                                    placeAnchorPos,
                                                    oldItemDataList,
                                                    swapPlaceMode);

                    case ESwapState.SmallToLarge:
                        return SwapSmallToLargeItem(plan, placeAnchorPos, swapPlaceMode);

                    default:
                        return false;
                }
            }

            #endregion

            #region 目标检测

            /// <summary>
            /// 获取交换目标物品
            /// </summary>
            /// <param name="dragItemData">拖动物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapTargetItem">交换目标</param>
            /// <returns>是否找到目标</returns>
            public bool TryGetSwapTargetItem(IItemRuntime dragItemData,
                                             Vector2Int placeAnchorPos,
                                             out IItemRuntime swapTargetItem)
            {
                swapTargetItem = null;
                if (dragItemData is null) return false;

                var dragSize = dragItemData.DataSize;
                if (dragSize.x <= 0 || dragSize.y <= 0) return false;

                var overlapItemHashList = new HashSet<IItemRuntime>();
                for (int x = 0; x < dragSize.x; x++)
                {
                    for (int y = 0; y < dragSize.y; y++)
                    {
                        var pos = new Vector2Int(placeAnchorPos.x + x, placeAnchorPos.y + y);
                        if (!inventoryState.IsInside(pos)) return false;

                        var overlapItem = inventoryState.GetItemByMask(pos);
                        if (overlapItem is null) continue;
                        if (overlapItem.InstancedItemId == dragItemData.InstancedItemId) continue;
                        overlapItemHashList.Add(overlapItem);
                    }
                }

                if (overlapItemHashList.Count == 0) return false;

                IItemRuntime fullCoveredItem = null;
                foreach (var overlapItem in overlapItemHashList)
                {
                    var swapPlan = GetSwapPlan(dragItemData, overlapItem);
                    if (swapPlan.SwapState == ESwapState.CanNotSwap)
                        return false;

                    if (swapPlan.SwapState == ESwapState.SmallToLarge)
                    {
                        if (fullCoveredItem is not null &&
                            fullCoveredItem.InstancedItemId != overlapItem.InstancedItemId)
                            return false;

                        fullCoveredItem = overlapItem;
                        continue;
                    }

                    var overlapSize = overlapItem.DataSize;
                    var overlapAnchorPos = overlapItem.AnchorPos;
                    bool isFullyCovered = true;
                    for (int x = 0; x < overlapSize.x && isFullyCovered; x++)
                    {
                        for (int y = 0; y < overlapSize.y; y++)
                        {
                            var overlapPos = new Vector2Int(overlapAnchorPos.x + x, overlapAnchorPos.y + y);
                            bool coveredByDrag =
                                overlapPos.x >= placeAnchorPos.x &&
                                overlapPos.x < placeAnchorPos.x + dragSize.x &&
                                overlapPos.y >= placeAnchorPos.y &&
                                overlapPos.y < placeAnchorPos.y + dragSize.y;
                            if (!coveredByDrag)
                            {
                                isFullyCovered = false;
                                break;
                            }
                        }
                    }

                    if (!isFullyCovered) return false;
                    if (fullCoveredItem is null)
                        fullCoveredItem = overlapItem;
                }

                swapTargetItem = fullCoveredItem;
                return swapTargetItem is not null;
            }

            #endregion

            #region 交换计划

            /// <summary>
            /// 构建交换计划
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <returns>交换计划</returns>
            private SwapPlan GetSwapPlan(IItemRuntime aItemData, IItemRuntime bItemData)
            {
                if (aItemData is null || bItemData is null)
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                if (aItemData.InstancedItemId == bItemData.InstancedItemId)
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                var aAnchorPos = aItemData.AnchorPos;
                var bAnchorPos = bItemData.AnchorPos;

                if (aAnchorPos == bAnchorPos)
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                if (!inventoryState.IsInside(aAnchorPos) || !inventoryState.IsInside(bAnchorPos))
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                SwapPlan swapPlan = new();
                var aSize = aItemData.DataSize.x * aItemData.DataSize.y;
                var bSize = bItemData.DataSize.x * bItemData.DataSize.y;

                if (aSize == bSize)
                {
                    swapPlan.SwapState = ESwapState.Same;
                    swapPlan.aItemData = aItemData;
                    swapPlan.bItemData = bItemData;
                    return swapPlan;
                }

                if (aSize > bSize)
                {
                    swapPlan.SwapState = ESwapState.LargeToSmall;
                    swapPlan.aItemData = aItemData;
                    swapPlan.bItemData = bItemData;
                    return swapPlan;
                }

                swapPlan.SwapState = ESwapState.SmallToLarge;
                swapPlan.aItemData = aItemData;
                swapPlan.bItemData = bItemData;
                return swapPlan;
            }

            #endregion

            #region 交换分支

            /// <summary>
            /// 相同面积交换
            /// </summary>
            /// <param name="plan">交换计划</param>
            /// <param name="aIndex">A索引</param>
            /// <param name="bIndex">B索引</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            public bool SwapSameItem(SwapPlan plan,
                                     int aIndex,
                                     int bIndex,
                                     Vector2Int placeAnchorPos,
                                     ESwapPlaceMode swapPlaceMode)
            {
                var aItemData = plan.aItemData;
                var bItemData = plan.bItemData;

                if (aItemData.IsRotated != bItemData.IsRotated)
                    return false;

                if (swapPlaceMode == ESwapPlaceMode.CrossContainer)
                {
                    inventoryState.RemoveAt(bItemData.AnchorPos);
                    if (!inventoryState.CanPlace(aItemData, placeAnchorPos))
                        return false;

                    inventoryState.SetItemData(aItemData, placeAnchorPos);
                    return true;
                }

                inventoryState.itemAnchorArray[aIndex] = null;
                inventoryState.itemAnchorArray[bIndex] = null;
                inventoryState.WriteOccupancy(aItemData, aItemData.AnchorPos, false);
                inventoryState.WriteOccupancy(bItemData, bItemData.AnchorPos, false);

                if (!inventoryState.CanPlace(aItemData, bItemData.AnchorPos))
                    return false;
                inventoryState.SetItemData(aItemData, bItemData.AnchorPos);

                if (!inventoryState.CanPlace(bItemData, aItemData.AnchorPos))
                    return false;
                inventoryState.SetItemData(bItemData, aItemData.AnchorPos);

                return true;
            }

            /// <summary>
            /// 大物品交换小物品
            /// </summary>
            /// <param name="plan">交换计划</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            public bool SwapLargeToSmallItem(SwapPlan plan,
                                             Vector2Int placeAnchorPos,
                                             List<IItemRuntime> oldItemDataList,
                                             ESwapPlaceMode swapPlaceMode)
            {
                tempLittleItemHashList.Clear();
                tempLittleItemOffsetDict.Clear();

                var largeItemData = plan.aItemData;
                var largeSize = largeItemData.DataSize;

                for (int x = 0; x < largeSize.x; x++)
                {
                    for (int y = 0; y < largeSize.y; y++)
                    {
                        var coverPos = new Vector2Int(placeAnchorPos.x + x, placeAnchorPos.y + y);
                        var littleItem = inventoryState.GetItemByMask(coverPos);
                        if (littleItem is not null && littleItem.InstancedItemId != plan.aItemData.InstancedItemId)
                        {
                            if (!tempLittleItemHashList.Add(littleItem))
                                continue;

                            tempLittleItemOffsetDict.Add(littleItem, littleItem.AnchorPos - placeAnchorPos);
                            oldItemDataList.Add(littleItem);
                        }
                    }
                }

                if (swapPlaceMode == ESwapPlaceMode.SameContainer)
                    inventoryState.RemoveAt(largeItemData.AnchorPos);
                foreach (var littleItem in tempLittleItemHashList)
                    inventoryState.RemoveAt(littleItem.AnchorPos);

                if (!inventoryState.CanPlace(largeItemData, placeAnchorPos))
                    return false;

                inventoryState.SetItemData(largeItemData, placeAnchorPos);

                if (swapPlaceMode == ESwapPlaceMode.CrossContainer)
                    return true;

                foreach (var littleItem in tempLittleItemHashList)
                {
                    if (!tempLittleItemOffsetDict.TryGetValue(littleItem, out var relativeOffset))
                        return false;

                    var targetAnchorPos = largeItemData.AnchorPos + relativeOffset;
                    if (!inventoryState.CanPlace(littleItem, targetAnchorPos))
                        return false;

                    inventoryState.SetItemData(littleItem, targetAnchorPos);
                }

                return true;
            }

            /// <summary>
            /// 小物品交换大物品
            /// </summary>
            /// <param name="plan">交换计划</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            public bool SwapSmallToLargeItem(SwapPlan plan,
                                             Vector2Int placeAnchorPos,
                                             ESwapPlaceMode swapPlaceMode)
            {
                var smallItemData = plan.aItemData;
                var largeItemData = plan.bItemData;

                if (swapPlaceMode == ESwapPlaceMode.SameContainer)
                    inventoryState.RemoveAt(smallItemData.AnchorPos);
                inventoryState.RemoveAt(largeItemData.AnchorPos);
                if (!inventoryState.CanPlace(smallItemData, placeAnchorPos))
                    return false;

                inventoryState.SetItemData(smallItemData, placeAnchorPos);
                if (swapPlaceMode == ESwapPlaceMode.CrossContainer)
                    return true;

                if (!inventoryState.CanPlace(largeItemData, smallItemData.AnchorPos))
                {
                    if (inventoryState.SetAtFirst(largeItemData, out _))
                        return true;
                    return false;
                }

                inventoryState.SetItemData(largeItemData, smallItemData.AnchorPos);
                return true;
            }

            #endregion
        }
    }
}
