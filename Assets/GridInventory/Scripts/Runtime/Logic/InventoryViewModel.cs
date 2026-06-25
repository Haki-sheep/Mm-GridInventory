using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{

    /// <summary>
    /// 操作结果结构体
    /// </summary>
    public readonly struct InventoryOpReport
    {
        /// <summary> 是否操作成功 </summary>
        public readonly bool IsSuccess;

        /// <summary> 旧物品数据 </summary>
        public readonly ItemRtData OldItemData;

        /// <summary> 新物品数据 </summary>
        public readonly ItemRtData NewItemData;

        /// <summary> 旧物品数据列表 </summary>
        public readonly List<ItemRtData> OldItemDataList;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="oldItemData"></param>
        /// <param name="newItemData"></param>
        /// <param name="oldItemDataList"></param>
        public InventoryOpReport(bool isSuccess,
                                 ItemRtData oldItemData,
                                 ItemRtData newItemData = null,
                                 List<ItemRtData> oldItemDataList = null)
        {
            IsSuccess = isSuccess;
            OldItemData = oldItemData;
            NewItemData = newItemData;
            OldItemDataList = oldItemDataList;
        }
    }


    /// <summary>
    /// 此类的职责是 接收View的请求 调用Model的算法 返回操作结果
    /// 因为Mdoel只是负责算数据上这块能不能放
    /// 但是View需要知道操作是否成功 并且做出对应的表现
    /// </summary>

    public class InventoryViewModel
    {
        private InventoryState inventoryState;

        public void Init(Vector2Int gridSize)
        {
            inventoryState = new InventoryState(gridSize);
        }

        /// <summary>
        /// 尝试放置物品
        /// </summary>
        /// <param name="oldItemData"></param>
        /// <param name="newAnchorPos"></param>
        /// <returns></returns>
        public InventoryOpReport TryPlaceItem(ItemRtData oldItemData,
                                              Vector2Int oldAnchorPos,
                                              Vector2Int newAnchorPos)
        {
            if (oldItemData is null)
                return new InventoryOpReport(false, null);

            void RestoreOldItem()
            {
                oldItemData.SetAnchorPos(oldAnchorPos);
                inventoryState.SetAt(oldAnchorPos, oldItemData);
            }

            // 直接放
            if (inventoryState.CanPlace(oldItemData, newAnchorPos))
            {
                oldItemData.SetAnchorPos(newAnchorPos);
                if (!inventoryState.SetAt(newAnchorPos, oldItemData))
                {
                    RestoreOldItem();
                    return new InventoryOpReport(false, oldItemData);
                }
                return new InventoryOpReport(true, oldItemData);
            }

            var targetItemData = inventoryState.GetItemByMask(newAnchorPos);

            // 尝试堆叠
            if (targetItemData != null
                && inventoryState.CanStack(oldItemData, targetItemData)
                && inventoryState.TryStack(oldItemData, targetItemData))
            {
                if (oldItemData.CurrStackCount > 0)
                {
                    oldItemData.SetAnchorPos(oldAnchorPos);
                    inventoryState.SetAt(oldAnchorPos, oldItemData);
                }

                return new InventoryOpReport(true, null, targetItemData as ItemRtData);
            }

            if (inventoryState.TryGetSwapTargetItem(oldItemData, newAnchorPos, out var swapTargetItem)
                && inventoryState.CanSwap(oldItemData, swapTargetItem, newAnchorPos))
            {
                var swapDisplacedList = new List<IItemRuntime>();
                if (inventoryState.TrySwap(oldItemData,
                                           swapTargetItem,
                                           swapDisplacedList,
                                           newAnchorPos))
                {
                    var oldItemDataList = ToItemRtDataList(swapDisplacedList);
                    return new InventoryOpReport(true, oldItemData, swapTargetItem as ItemRtData, oldItemDataList);
                }

                return new InventoryOpReport(false, oldItemData, swapTargetItem as ItemRtData);
            }

            // 全部尝试失败 回滚状态
            RestoreOldItem();
            return new InventoryOpReport(false, oldItemData);
        }

        public void PlaceItem(ItemRtData itemData, Vector2Int anchorPos)
        {
            if (itemData is null)
                return;

            itemData.SetAnchorPos(anchorPos);
            inventoryState.SetAt(anchorPos, itemData);
        }

        public bool CanStack(ItemRtData dragItem, ItemRtData targetItem) =>
            inventoryState.CanStack(dragItem, targetItem);

        /// <summary>
        /// IGridItem列表转ItemRtData列表
        /// </summary>
        private static List<ItemRtData> ToItemRtDataList(List<IItemRuntime> gridItemList)
        {
            var itemRtDataList = new List<ItemRtData>(gridItemList.Count);
            for (int i = 0; i < gridItemList.Count; i++)
                itemRtDataList.Add((ItemRtData)gridItemList[i]);
            return itemRtDataList;
        }

        /// <summary>
        /// 尝试移除物品
        /// </summary>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public InventoryOpReport TryRemoveItem(Vector2Int anchorPos)
        {
            var item = inventoryState.GetItemByMask(anchorPos) as ItemRtData;
            if (item is null)
            {
                return new InventoryOpReport(false, null);
            }
            if (!inventoryState.RemoveAtAny(anchorPos))
            {
                return new InventoryOpReport(false, null);
            }
            return new InventoryOpReport(true, item);
        }

        /// <summary>
        /// 获取任意格上的物品
        /// </summary>
        /// <param name="anyPos"></param>
        /// <returns></returns>
        public ItemRtData GetItemAt(Vector2Int anyPos)
        {
            return inventoryState.GetItemByMask(anyPos) as ItemRtData;
        }

        /// <summary>
        /// 判断是否可以放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public bool CanPlaceItem(ItemRtData itemData, Vector2Int anchorPos)
        {
            return inventoryState.CanPlace(itemData, anchorPos);
        }


        /// <summary>
        /// 尝试旋转物品
        /// 这里只是转变数据状态 不影响实际物品的旋转
        /// </summary>
        /// <param name="itemData"></param>
        public InventoryOpReport TryRotateItem(ItemRtData itemData)
        {
            if (itemData is null)
                return new InventoryOpReport(false, null);

            var originData = ItemRtDataMgr.Instance.GetItemData<IItemBaseData>(itemData.ExcelItemId);

            // 可叠加物品不允许旋转（默认会配成正方形）
            if (originData is not null && originData.ItemStackType == EItemStackType.Stackable)
                return new InventoryOpReport(false, itemData);

            itemData.SetRotated(!itemData.IsRotated);
            return new InventoryOpReport(true, itemData);
        }


        /// <summary>
        /// 获取操作类型    
        /// </summary>
        /// <returns></returns>
        public EFrameBoard JudgeFrameBoardState(ItemRtData oldItemData,
                                            ItemRtData newItemData,
                                            Vector2Int dragPreviewAnchorPos)
        {
            if (CanPlaceItem(oldItemData, dragPreviewAnchorPos))
            {
                return EFrameBoard.CanPlace;
            }
            else if (newItemData is not null && CanStack(oldItemData, newItemData))
            {
                return EFrameBoard.CanStack;
            }
            else if (inventoryState.TryGetSwapTargetItem(oldItemData, dragPreviewAnchorPos, out var swapTargetItem) &&
                     inventoryState.CanSwap(oldItemData, swapTargetItem, dragPreviewAnchorPos))
            {
                return EFrameBoard.CanPlaceSwap;
            }
            else
            {
                return EFrameBoard.CannotPlace;
            }
        }
    }
}
