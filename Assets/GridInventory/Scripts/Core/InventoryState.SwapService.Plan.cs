using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public partial class InventoryState
    {
        private sealed partial class InventorySwapService
        {
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
        }
    }
}
