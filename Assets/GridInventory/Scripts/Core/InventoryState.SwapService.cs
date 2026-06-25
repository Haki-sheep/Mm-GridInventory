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
            private readonly HashSet<ItemRtData> tempLittleItemHashList = new();
            /// <summary> 试算用临时列表 </summary>
            private readonly List<ItemRtData> simulateOldItemList = new();

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
            public bool CanSwap(ItemRtData aItemData,
                                ItemRtData bItemData,
                                Vector2Int placeAnchorPos) =>
            SimulateSwap(aItemData, bItemData, placeAnchorPos);

            /// <summary>
            /// 执行交换
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <returns>是否成功</returns>
            public bool TrySwap(ItemRtData aItemData,
                                ItemRtData bItemData,
                                List<ItemRtData> oldItemDataList,
                                Vector2Int placeAnchorPos) =>
            CommitSwap(aItemData, bItemData, placeAnchorPos, oldItemDataList);
            #endregion


            #region 内部方法
            /// <summary>
            /// 在克隆网格上试算交换
            /// 不会修改真实背包数据
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <returns>是否可以交换</returns>
            private bool SimulateSwap(ItemRtData aItemData,
                                      ItemRtData bItemData,
                                      Vector2Int placeAnchorPos)
            {
                // 获取交换计划
                var plan = GetSwapPlan(aItemData, bItemData);
                if (plan.SwapState == ESwapState.CanNotSwap) return false;

                // 获取交换物品的锚点索引
                int aIndex = inventoryState.ToIndex(plan.aItemData.AnchorPos);
                int bIndex = inventoryState.ToIndex(plan.bItemData.AnchorPos);

                // 克隆物品数组和占用信息
                var simAnchorArray = (ItemRtData[])inventoryState.itemAnchorArray.Clone();
                var simOccupancyArray = (ItemRtData[])inventoryState.occupancyOwnerArray.Clone();
                // 真实物品数组和占用信息
                var realAnchorArray = inventoryState.itemAnchorArray;
                // 真实占用信息
                var realOccupancyArray = inventoryState.occupancyOwnerArray;

                // 临时交换物品数组和占用信息
                inventoryState.itemAnchorArray = simAnchorArray;
                inventoryState.occupancyOwnerArray = simOccupancyArray;
                try
                {
                    // 尝试执行交换
                    return TryExecuteSwap(plan, aIndex, bIndex, placeAnchorPos, simulateOldItemList);
                }
                // 如果交换失败 则回滚真实网格
                finally
                {
                    inventoryState.itemAnchorArray = realAnchorArray;
                    inventoryState.occupancyOwnerArray = realOccupancyArray;
                }
                // 返回是否可以交换
            }

            /// <summary>
            /// 提交交换 失败时回滚真实网格
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <returns>是否成功</returns>
            private bool CommitSwap(ItemRtData aItemData,
                                    ItemRtData bItemData,
                                    Vector2Int placeAnchorPos,
                                    List<ItemRtData> oldItemDataList)
            {
                // 获取交换计划
                var plan = GetSwapPlan(aItemData, bItemData);
                if (plan.SwapState == ESwapState.CanNotSwap)
                    return false;

                // 获取交换物品的锚点索引
                int aIndex = inventoryState.ToIndex(plan.aItemData.AnchorPos);
                int bIndex = inventoryState.ToIndex(plan.bItemData.AnchorPos);

                // 备份物品数组和占用信息
                var backupItemArray = (ItemRtData[])inventoryState.itemAnchorArray.Clone();
                var backupOccupancyOwner = (ItemRtData[])inventoryState.occupancyOwnerArray.Clone();

                // 是否需要回滚标记
                bool shouldRollback = true;
                try
                {
                    // 尝试执行交换
                    var canSwap = TryExecuteSwap(plan, aIndex, bIndex, placeAnchorPos, oldItemDataList);
                    if (!canSwap) return false;

                    // 更新物品信息
                    for (int i = 0; i < inventoryState.itemAnchorArray.Length; i++)
                    {
                        // 获取物品
                        var item = inventoryState.itemAnchorArray[i];
                        if (item is null) continue;
                        // 更新物品锚点
                        item.SetAnchorPos(inventoryState.ToVector2Int(i));
                    }
                    // 不需要回滚
                    shouldRollback = false;
                    return true;
                }
                // 如果交换失败 则回滚真实网格
                finally
                {
                    // 如果需要回滚 则回滚物品数组和占用信息
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
                                        List<ItemRtData> oldItemDataList)
            {
                oldItemDataList.Clear();
                switch (plan.SwapState)
                {
                    case ESwapState.Same:
                        return SwapSameItem(plan, aIndex, bIndex);

                    case ESwapState.LargeToSmall:
                        return SwapLargeToSmallItem(plan, placeAnchorPos, oldItemDataList);

                    case ESwapState.SmallToLarge:
                        return SwapSmallToLargeItem(plan, placeAnchorPos);

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
            public bool TryGetSwapTargetItem(ItemRtData dragItemData,
                                             Vector2Int placeAnchorPos,
                                             out ItemRtData swapTargetItem)
            {
                swapTargetItem = null;
                if (dragItemData is null) return false;

                var dragSize = dragItemData.DataSize;
                if (dragSize.x <= 0 || dragSize.y <= 0) return false;

                var overlapItemHashList = new HashSet<ItemRtData>();
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

                ItemRtData fullCoveredItem = null;
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

                // 如果旋转状态不同 则不能交换
                if (aItemData.IsRotated != bItemData.IsRotated)
                    return false;

                // 清空锚点物品
                inventoryState.itemAnchorArray[aIndex] = null;
                inventoryState.itemAnchorArray[bIndex] = null;
                // 清空占用信息
                inventoryState.WriteOccupancy(aItemData, aItemData.AnchorPos, false);
                inventoryState.WriteOccupancy(bItemData, bItemData.AnchorPos, false);

                // 试试a能不能放到b的位置
                if (!inventoryState.CanPlace(aItemData, bItemData.AnchorPos))
                    return false;
                // 如果a能放 则把a放到b的位置
                inventoryState.SetItemData(aItemData, bItemData.AnchorPos);

                // 试试b能不能放到a的位置
                if (!inventoryState.CanPlace(bItemData, aItemData.AnchorPos))
                    return false;
                // 如果b能放 则把b放到a的位置
                inventoryState.SetItemData(bItemData, aItemData.AnchorPos);

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
                                             List<ItemRtData> oldItemDataList)
            {
                tempLittleItemHashList.Clear();
                
                // 获取大物品信息
                var largeItemData = plan.aItemData;
                var largeSize = largeItemData.DataSize;

                // 获取大物品覆盖的物品
                for (int x = 0; x < largeSize.x; x++)
                {
                    for (int y = 0; y < largeSize.y; y++)
                    {
                        var coverPos = new Vector2Int(placeAnchorPos.x + x, placeAnchorPos.y + y);
                        var littleItem = inventoryState.GetItemByMask(coverPos);
                        // 如果物品不为空 并且物品不是大物品 则添加到临时物品集合
                        if (littleItem is not null && littleItem.InstancedItemId != plan.aItemData.InstancedItemId)
                        {
                            // 添加到临时物品集合
                            tempLittleItemHashList.Add(littleItem);
                            // 添加到旧物品列表 用于UI层使用
                            oldItemDataList.Add(littleItem);
                        }
                    }
                }

                // 移除大物品
                inventoryState.RemoveAt(largeItemData.AnchorPos);
                // 移除被覆盖的小物品
                foreach (var littleItem in tempLittleItemHashList)
                    inventoryState.RemoveAt(littleItem.AnchorPos);

                // 尝试放置大物品 如果不能放置 则交换失败
                if (!inventoryState.CanPlace(largeItemData, placeAnchorPos))
                    return false;

                // 放下大物品
                inventoryState.SetItemData(largeItemData, placeAnchorPos);

                foreach (var littleItem in tempLittleItemHashList)
                {
                    bool placed = false;

                    // 优先放回旧大物品区域
                    for (int i = largeItemData.AnchorPos.x; i < largeItemData.AnchorPos.x + largeItemData.DataSize.x && !placed; i++)
                    {
                        for (int j = largeItemData.AnchorPos.y; j < largeItemData.AnchorPos.y + largeItemData.DataSize.y; j++)
                        {
                            var candidate = new Vector2Int(i, j);
                            if (!inventoryState.CanPlace(littleItem, candidate)) continue;

                            inventoryState.SetItemData(littleItem, candidate);
                            placed = true;
                            break;
                        }
                    }

                    // 放不回原区域时 再尝试全背包首个空位放置
                    if (!placed && inventoryState.SetAtFirst(littleItem, out _))
                        placed = true;

                    // 如果既放不回原区域 又放不回全背包首个空位 则交换失败
                    if (!placed)
                        return false;
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

                inventoryState.SetItemData(smallItemData, placeAnchorPos);
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

            #region 交换计划
            /// <summary>
            /// 构建交换计划
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <returns>交换计划</returns>
            private SwapPlan GetSwapPlan(ItemRtData aItemData, ItemRtData bItemData)
            {
                if (aItemData is null || bItemData is null)
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                // 物品不要和自己交换 没有意义
                if (aItemData.InstancedItemId == bItemData.InstancedItemId)
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                var aAnchorPos = aItemData.AnchorPos;
                var bAnchorPos = bItemData.AnchorPos;

                // 原地交换也没有意义
                if (aAnchorPos == bAnchorPos)
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };

                // 检查物品是否在背包范围内
                if (!inventoryState.IsInside(aAnchorPos) || !inventoryState.IsInside(bAnchorPos))
                    return new SwapPlan { SwapState = ESwapState.CanNotSwap };


                // 构建交换计划
                SwapPlan swapPlan = new();
                var aSize = aItemData.DataSize.x * aItemData.DataSize.y;
                var bSize = bItemData.DataSize.x * bItemData.DataSize.y;

                // 构建不同面积的交换计划
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
