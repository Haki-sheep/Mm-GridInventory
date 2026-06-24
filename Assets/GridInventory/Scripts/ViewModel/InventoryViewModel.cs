using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{

    public readonly struct InventoryOpResult
    {
        public readonly bool IsSuccess;
        public readonly RunTimeItemData OldItemData;
        public readonly RunTimeItemData NewItemData;
        public readonly List<RunTimeItemData> OldItemDataList;

        public InventoryOpResult(bool isSuccess, RunTimeItemData oldItemData, RunTimeItemData newItemData = null, List<RunTimeItemData> oldItemDataList = null)
        {
            IsSuccess = isSuccess;
            OldItemData = oldItemData;
            NewItemData = newItemData;
            OldItemDataList = oldItemDataList;
        }
    }


    /// <summary>
    /// 这里做统一入口 接收View的请求 调用Model的算法 返回结果
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
        public InventoryOpResult TryPlaceItem(RunTimeItemData oldItemData,
                                              Vector2Int oldAnchorPos,
                                              Vector2Int newAnchorPos)
        {
            if (oldItemData is null)
                return new InventoryOpResult(false, null);

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
                    return new InventoryOpResult(false, oldItemData);
                }
                Debug.Log($"直接放置物品成功 物品新锚点：{newAnchorPos}");
                return new InventoryOpResult(true, oldItemData);
            }

            var newItemData = inventoryState.GetItemByMask(newAnchorPos);

            // 尝试堆叠
            if (inventoryState.CanStack(oldItemData, newItemData, out int remainingCount))
            {
                // 更新新旧物品的计数
                newItemData.SetStackCount(oldItemData.CurStackCount + newItemData.CurStackCount);
                oldItemData.SetStackCount(remainingCount);

                // 如果剩余数量为0 则销毁旧物品
                if (remainingCount == 0)
                {
                    inventoryState.RemoveAtAny(oldAnchorPos);
                }
                // 否则将旧物品放置到原锚点
                else
                {
                    inventoryState.SetAt(oldAnchorPos, oldItemData);
                }

                Debug.Log($"尝试堆叠物品成功 堆叠对象为{newItemData} / 堆叠到新锚点 {newItemData.AnchorPos}");
                return new InventoryOpResult(true, null, newItemData);
            }

            if (inventoryState.TryGetSwapTargetItem(oldItemData, newAnchorPos, out var swapTargetItem)
                && inventoryState.CanSwap(oldItemData, swapTargetItem, newAnchorPos))
            {
                if (inventoryState.TrySwap(oldItemData,
                                           swapTargetItem,
                                           out List<RunTimeItemData> oldItemDataList,
                                           newAnchorPos))
                {
                    foreach (var item in oldItemDataList)
                    {
                        Debug.Log($"交换物品成功 交换对象为{item} / 交换到新锚点 {item.AnchorPos}");
                    }
                    return new InventoryOpResult(true, oldItemData, swapTargetItem, oldItemDataList);
                }

                return new InventoryOpResult(false, oldItemData, swapTargetItem);
            }

            // 全部尝试失败 回滚状态
            RestoreOldItem();
            return new InventoryOpResult(false, oldItemData);
        }

        public void PlaceItem(RunTimeItemData itemData, Vector2Int anchorPos)
        {
            if (itemData is null)
                return;

            itemData.SetAnchorPos(anchorPos);
            inventoryState.SetAt(anchorPos, itemData);
        }

        public bool CanStack(RunTimeItemData oldItemData, RunTimeItemData newItemData, out int remainingCount)
        {
            return inventoryState.CanStack(oldItemData, newItemData, out remainingCount);
        }

        public bool CanSwap(RunTimeItemData oldItemData, RunTimeItemData newItemData)
        {
            Debug.Log($"CanSwap: {oldItemData.AnchorPos} {newItemData.AnchorPos}");
            return inventoryState.CanSwap(oldItemData, newItemData, oldItemData.AnchorPos);
        }

        /// <summary>
        /// 尝试移除物品
        /// </summary>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public InventoryOpResult TryRemoveItem(Vector2Int anchorPos)
        {
            var item = inventoryState.GetItemByMask(anchorPos);
            if (item is null)
            {
                return new InventoryOpResult(false, null);
            }
            if (!inventoryState.RemoveAtAny(anchorPos))
            {
                return new InventoryOpResult(false, null);
            }
            return new InventoryOpResult(true, item);
        }

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public bool FindPlaceAtFirstItem(RunTimeItemData itemData, out Vector2Int anchorPos)
        {
            return inventoryState.FindSetAtFirst(itemData, out anchorPos);
        }

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置 并放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public bool PlaceAtFirstItem(RunTimeItemData itemData, out Vector2Int anchorPos)
        {
            return inventoryState.SetAtFirst(itemData, out anchorPos);
        }

        /// <summary>
        /// 获取任意格上的物品
        /// </summary>
        /// <param name="anyPos"></param>
        /// <returns></returns>
        public RunTimeItemData GetItemAt(Vector2Int anyPos)
        {
            return inventoryState.GetItemByMask(anyPos);
        }

        /// <summary>
        /// 判断是否可以放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public bool CanPlaceItem(RunTimeItemData itemData, Vector2Int anchorPos)
        {
            return inventoryState.CanPlace(itemData, anchorPos);
        }


        /// <summary>
        /// 尝试旋转物品
        /// 这里只是转变数据状态 不影响实际物品的旋转
        /// </summary>
        /// <param name="itemData"></param>
        public InventoryOpResult TryRotateItem(RunTimeItemData itemData)
        {
            if (itemData is null)
                return new InventoryOpResult(false, null);

            var originData = RunTimeItemDataMgr.Instance.GetItemData<IItemRootData>(itemData.PersistenceItemId);

            // 可叠加物品不允许旋转（默认会配成正方形）
            if (originData is not null && originData.ItemStackType == EItemStackType.Stackable)
                return new InventoryOpResult(false, itemData);

            itemData.SetRotated(!itemData.IsRotated);
            return new InventoryOpResult(true, itemData);
        }


        /// <summary>
        /// 获取操作类型    
        /// </summary>
        /// <returns></returns>
        public EFrameBoard JudgeFrameBoardState(RunTimeItemData oldItemData,
                                            RunTimeItemData newItemData,
                                            Vector2Int dragPreviewAnchorPos)
        {
            if (CanPlaceItem(oldItemData, dragPreviewAnchorPos))
            {
                return EFrameBoard.CanPlace;
            }
            else if (CanStack(oldItemData, newItemData, out int _))
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
