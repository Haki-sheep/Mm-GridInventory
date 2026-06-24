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
            private readonly HashSet<RunTimeItemData> tempItemHashList = new();

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
            /// <returns>是否可交换</returns>
            public bool CanSwap(RunTimeItemData aItemData,
                                RunTimeItemData bItemData,
                                Vector2Int placeAnchorPos) =>
            SwapItem(aItemData, bItemData, placeAnchorPos, commit: false, out _);

            /// <summary>
            /// 执行交换
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <returns>是否成功</returns>
            public bool TrySwap(RunTimeItemData aItemData,
                                RunTimeItemData bItemData,
                                out List<RunTimeItemData> oldItemDataList,
                                Vector2Int placeAnchorPos) =>
            SwapItem(aItemData, bItemData, placeAnchorPos, commit: true, out oldItemDataList);

            /// <summary>
            /// 通用交换执行入口
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="commit">是否提交</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <returns>是否成功</returns>
            public bool SwapItem(RunTimeItemData aItemData,
                                 RunTimeItemData bItemData,
                                 Vector2Int placeAnchorPos,
                                 bool commit,
                                 out List<RunTimeItemData> oldItemDataList)
            {
                oldItemDataList = new List<RunTimeItemData>();

                var plan = GetSwapPlan(aItemData, bItemData);
                if (plan.SwapState == ESwapState.CanNotSwap) return false;

                int aIndex = inventoryState.ToIndex(plan.aItemData.AnchorPos);
                int bIndex = inventoryState.ToIndex(plan.bItemData.AnchorPos);

                var backupItemArray = (RunTimeItemData[])inventoryState.runTimeItemDataArray.Clone();
                var backupOccupancyOwner = (RunTimeItemData[])inventoryState.occupancyOwnerArray.Clone();

                bool canSwap = false;
                bool shouldRollback = true;
                try
                {
                    switch (plan.SwapState)
                    {
                        case ESwapState.Same:
                            canSwap = SwapSameItem(plan, aIndex, bIndex);
                            break;

                        case ESwapState.LargeToSmall:
                            canSwap = SwapLargeToSmallItem(plan,
                                                           placeAnchorPos,
                                                           out oldItemDataList);
                            break;

                        case ESwapState.SmallToLarge:
                            canSwap = SwapSmallToLargeItem(plan, placeAnchorPos);
                            break;
                    }

                    if (canSwap && commit)
                    {
                        // 提交交换后 统一回写锚点
                        for (int i = 0; i < inventoryState.runTimeItemDataArray.Length; i++)
                        {
                            var item = inventoryState.runTimeItemDataArray[i];
                            if (item is null) continue;
                            item.SetAnchorPos(inventoryState.ToPosition(i));
                        }
                        shouldRollback = false;
                    }

                    return canSwap;
                }
                finally
                {
                    if (shouldRollback)
                    {
                        // 预览模式 或 交换失败时回滚快照
                        inventoryState.runTimeItemDataArray = backupItemArray;
                        inventoryState.occupancyOwnerArray = backupOccupancyOwner;
                    }
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
            public bool TryGetSwapTargetItem(RunTimeItemData dragItemData,
                                             Vector2Int placeAnchorPos,
                                             out RunTimeItemData swapTargetItem)
            {
                swapTargetItem = null;
                if (dragItemData is null) return false;

                var dragSize = inventoryState.GetOccupiedSize(dragItemData);
                if (dragSize.x <= 0 || dragSize.y <= 0) return false;

                var overlapItemHashList = new HashSet<RunTimeItemData>();
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

                RunTimeItemData fullCoveredItem = null;
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

                    var overlapSize = inventoryState.GetOccupiedSize(overlapItem);
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

            #region 交换分支
            /// <summary>
            /// 相同面积交换
            /// </summary>
            /// <param name="plan">交换计划</param>
            /// <param name="aIndex">A索引</param>
            /// <param name="bIndex">B索引</param>
            /// <returns>是否成功</returns>
            public bool SwapSameItem(SwapPlan plan, int aIndex, int bIndex)
            {
                var aItemData = plan.aItemData;
                var bItemData = plan.bItemData;
                if (aItemData.IsRotated != bItemData.IsRotated)
                    return false;

                inventoryState.runTimeItemDataArray[aIndex] = null;
                inventoryState.runTimeItemDataArray[bIndex] = null;
                inventoryState.WriteOccupancy(aItemData, aItemData.AnchorPos, false);
                inventoryState.WriteOccupancy(bItemData, bItemData.AnchorPos, false);

                if (!inventoryState.CanPlace(aItemData, bItemData.AnchorPos))
                {
                    return false;
                }

                inventoryState.SetAnchorItem(bItemData.AnchorPos, aItemData);
                if (!inventoryState.CanPlace(bItemData, aItemData.AnchorPos))
                {
                    return false;
                }

                inventoryState.SetAnchorItem(aItemData.AnchorPos, bItemData);
                return true;
            }

            /// <summary>
            /// 大物品交换小物品
            /// </summary>
            /// <param name="plan">交换计划</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <returns>是否成功</returns>
            public bool SwapLargeToSmallItem(SwapPlan plan,
                                             Vector2Int placeAnchorPos,
                                             out List<RunTimeItemData> oldItemDataList)
            {
                oldItemDataList = new List<RunTimeItemData>();
                tempItemHashList.Clear();

                var largeItemData = plan.aItemData;
                var largeSize = inventoryState.GetOccupiedSize(largeItemData);
                for (int x = 0; x < largeSize.x; x++)
                {
                    for (int y = 0; y < largeSize.y; y++)
                    {
                        var pos = new Vector2Int(placeAnchorPos.x + x, placeAnchorPos.y + y);
                        var item = inventoryState.GetItemByMask(pos);
                        if (item is not null && item.InstancedItemId != plan.aItemData.InstancedItemId)
                        {
                            tempItemHashList.Add(item);
                            oldItemDataList.Add(item);
                        }
                    }
                }

                inventoryState.RemoveAt(largeItemData.AnchorPos);
                foreach (var item in tempItemHashList)
                    inventoryState.RemoveAt(item.AnchorPos);

                if (!inventoryState.CanPlace(largeItemData, placeAnchorPos))
                {
                    return false;
                }

                inventoryState.SetAnchorItem(placeAnchorPos, largeItemData);
                foreach (var item in tempItemHashList)
                {
                    bool placed = false;

                    // 优先放回旧大物品区域
                    for (int i = largeItemData.AnchorPos.x; i < largeItemData.AnchorPos.x + largeItemData.DataSize.x && !placed; i++)
                    {
                        for (int j = largeItemData.AnchorPos.y; j < largeItemData.AnchorPos.y + largeItemData.DataSize.y; j++)
                        {
                            var candidate = new Vector2Int(i, j);
                            if (!inventoryState.CanPlace(item, candidate)) continue;

                            inventoryState.SetAnchorItem(candidate, item);
                            placed = true;
                            break;
                        }
                    }

                    // 放不回原区域时 再走全背包首个空位
                    if (!placed && inventoryState.FindSetAtFirst(item, out Vector2Int anchorPos))
                    {
                        inventoryState.SetAnchorItem(anchorPos, item);
                        placed = true;
                    }

                    if (!placed)
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// 小物品交换大物品
            /// </summary>
            /// <param name="plan">交换计划</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <returns>是否成功</returns>
            public bool SwapSmallToLargeItem(SwapPlan plan, Vector2Int placeAnchorPos)
            {
                var smallItemData = plan.aItemData;
                var largeItemData = plan.bItemData;

                inventoryState.RemoveAt(smallItemData.AnchorPos);
                inventoryState.RemoveAt(largeItemData.AnchorPos);
                if (!inventoryState.CanPlace(smallItemData, placeAnchorPos))
                    return false;

                inventoryState.SetAnchorItem(placeAnchorPos, smallItemData);
                if (!inventoryState.CanPlace(largeItemData, smallItemData.AnchorPos))
                {
                    if (inventoryState.FindSetAtFirst(largeItemData, out Vector2Int anchorPos))
                    {
                        inventoryState.SetAnchorItem(anchorPos, largeItemData);
                        return true;
                    }
                    return false;
                }

                inventoryState.SetAnchorItem(smallItemData.AnchorPos, largeItemData);
                return true;
            }
            #endregion

            #region 交换计划
            /// <summary>
            /// 构建交换计划
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <returns>交换计划</returns>
            public SwapPlan GetSwapPlan(RunTimeItemData aItemData, RunTimeItemData bItemData)
            {
                if (aItemData == null || bItemData == null) return new SwapPlan { SwapState = ESwapState.CanNotSwap };
                if (aItemData.InstancedItemId == bItemData.InstancedItemId) return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                var aAnchorPos = aItemData.AnchorPos;
                var bAnchorPos = bItemData.AnchorPos;
                if (!inventoryState.IsInside(aAnchorPos) || !inventoryState.IsInside(bAnchorPos)) return new SwapPlan { SwapState = ESwapState.CanNotSwap };
                if (aAnchorPos == bAnchorPos) return new SwapPlan { SwapState = ESwapState.CanNotSwap };

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
                else if (aSize > bSize)
                {
                    swapPlan.SwapState = ESwapState.LargeToSmall;
                    swapPlan.aItemData = aItemData;
                    swapPlan.bItemData = bItemData;
                    return swapPlan;
                }
                else
                {
                    swapPlan.SwapState = ESwapState.SmallToLarge;
                    swapPlan.aItemData = aItemData;
                    swapPlan.bItemData = bItemData;
                    return swapPlan;
                }
            }
            #endregion
        }
    }
}
