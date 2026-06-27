using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public partial class InventoryState
    {
        private sealed partial class InventorySwapService
        {
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

                // 如果旋转状态不同 则不能交换
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
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            public bool SwapLargeToSmallItem(SwapPlan plan,
                                             Vector2Int placeAnchorPos,
                                             List<IItemRuntime> oldItemDataList,
                                             ESwapPlaceMode swapPlaceMode)
            {
                tempLittleItemHashList.Clear();
                tempLittleItemOffsetDict.Clear();

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
                            // 同一个多格物品只记录一次
                            if (!tempLittleItemHashList.Add(littleItem))
                                continue;

                            // 记录小物品在新落点覆盖区域内的相对偏移
                            tempLittleItemOffsetDict.Add(littleItem, littleItem.AnchorPos - placeAnchorPos);
                            // 添加到旧物品列表 用于UI层使用
                            oldItemDataList.Add(littleItem);
                        }
                    }
                }

                // 移除大物品
                if (swapPlaceMode == ESwapPlaceMode.SameContainer)
                    inventoryState.RemoveAt(largeItemData.AnchorPos);
                // 移除被覆盖的小物品
                foreach (var littleItem in tempLittleItemHashList)
                    inventoryState.RemoveAt(littleItem.AnchorPos);

                // 尝试放置大物品 如果不能放置 则交换失败
                if (!inventoryState.CanPlace(largeItemData, placeAnchorPos))
                    return false;

                // 放下大物品
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
