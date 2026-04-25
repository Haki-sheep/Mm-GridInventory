using UnityEngine;
using System.Collections.Generic;

namespace MmInventory
{
    /// <summary>
    /// 放置服务 这里做统一入口
    /// 当前只保留统一入口，后续再逐步补齐冲突、堆叠、交换判定
    /// </summary>
    public class GridPlacementService
    {
        private InventoryState inventoryState;

        /// <summary>
        /// 初始化背包大小
        /// </summary>
        /// <param name="size"></param>
        public void InitSize(Vector2Int size)
        {
            inventoryState = new InventoryState(size);
        }

        /// <summary>
        /// 堆叠, 交换, 判断是否可以放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public bool CanPlace(RunTimeItemData itemData, Vector2Int anchorPos)
        {
            return inventoryState.CanPlace(itemData, anchorPos);
        }

        /// <summary>
        /// 获取掩码格子物品
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public RunTimeItemData GetItemByMask(Vector2Int position)
        {
            return inventoryState.GetItemByMask(position);
        }

        /// <summary>
        /// 放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        public bool PlaceItem(RunTimeItemData itemData, Vector2Int anchorPos)
        {
            return inventoryState.SetAt(anchorPos, itemData);
        }


        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置 并放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns> 是否成功 </returns>
        public bool PlaceAtFirst(RunTimeItemData itemData, out Vector2Int anchorPos)
        {
            return inventoryState.FindSetAtFirst(itemData, out anchorPos);
        }   

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>

        public bool FindPlaceAtFirst(RunTimeItemData itemData, out Vector2Int anchorPos)
        {
            return inventoryState.FindSetAtFirst(itemData, out anchorPos);
        }

        /// <summary>
        /// 移除任意格子物品
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool RemoveItemAny(Vector2Int position)
        {
            return inventoryState.RemoveAtAny(position);
        }

        public bool CanSwap(RunTimeItemData oldItemData,
                            RunTimeItemData newItemData,
                            Vector2Int? placeAnchorPos = null)
        {
            var targetAnchorPos = placeAnchorPos ?? oldItemData.AnchorPos;
            return inventoryState.CanSwap(oldItemData, newItemData, targetAnchorPos);
        }

        public bool TrySwap(RunTimeItemData oldItemData,
                            RunTimeItemData newItemData,
                            out List<RunTimeItemData> oldItemDataList,
                            Vector2Int? placeAnchorPos = null)
        {
            var targetAnchorPos = placeAnchorPos ?? oldItemData.AnchorPos;
            return inventoryState.TrySwap(oldItemData, newItemData, out oldItemDataList, targetAnchorPos);
        }

        public bool TryGetSwapTargetItem(RunTimeItemData dragItemData,
                                         Vector2Int placeAnchorPos,
                                         out RunTimeItemData swapTargetItem)
        {
            return inventoryState.TryGetSwapTargetItem(dragItemData, placeAnchorPos, out swapTargetItem);
        }

        public bool CanStack(RunTimeItemData oldItemData, RunTimeItemData newItemData, out int remainingCount)
        {
            return inventoryState.CanStack(oldItemData, newItemData, out remainingCount);
        }

    }
}
