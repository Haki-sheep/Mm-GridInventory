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
    /// 这里做统一入口 接收View的请求 调用Service的算法 返回结果
    /// </summary>

    public class InventoryViewModel
    {
        private GridPlacementService gridPlacementService = new();

        public void Init(Vector2Int gridSize)
        {
            gridPlacementService.InitSize(gridSize);
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
                gridPlacementService.PlaceItem(oldItemData, oldAnchorPos);
            }

            // 直接放
            if (gridPlacementService.CanPlace(oldItemData, newAnchorPos))
            {
                oldItemData.SetAnchorPos(newAnchorPos);
                if (!gridPlacementService.PlaceItem(oldItemData, newAnchorPos))
                {
                    RestoreOldItem();
                    return new InventoryOpResult(false, oldItemData);
                }
                Debug.Log($"直接放置物品成功 物品新锚点：{newAnchorPos}");
                return new InventoryOpResult(true, oldItemData);
            }

            var newItemData = gridPlacementService.GetItemByMask(newAnchorPos);

            // 尝试堆叠
            if (gridPlacementService.CanStack(oldItemData, newItemData, out int remainingCount))
            {
                // 更新新旧物品的计数
                newItemData.SetStackCount(oldItemData.CurStackCount + newItemData.CurStackCount);
                oldItemData.SetStackCount(remainingCount);

                // 如果剩余数量为0 则销毁旧物品
                if (remainingCount == 0)
                {
                    gridPlacementService.RemoveItemAny(oldAnchorPos);
                }
                // 否则将旧物品放置到原锚点
                else
                {
                    gridPlacementService.PlaceItem(oldItemData, oldAnchorPos);
                }

                Debug.Log($"尝试堆叠物品成功 堆叠对象为{newItemData} / 堆叠到新锚点 {newItemData.AnchorPos}");
                return new InventoryOpResult(true, null, newItemData);
            }

            if (gridPlacementService.TryGetSwapTargetItem(oldItemData, newAnchorPos, out var swapTargetItem)
                && gridPlacementService.CanSwap(oldItemData, swapTargetItem, newAnchorPos))
            {
                if (gridPlacementService.TrySwap(oldItemData,
                                                 swapTargetItem,
                                                 out List<RunTimeItemData> oldItemDataList,
                                                 newAnchorPos))
                {
                    // Debug.Log($"交换物品成功 交换对象为{newItemData} / 交换到新锚点 {newItemData.AnchorPos}");
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
            gridPlacementService.PlaceItem(itemData, anchorPos);
        }

        public bool CanStack(RunTimeItemData oldItemData, RunTimeItemData newItemData, out int remainingCount)
        {
            return gridPlacementService.CanStack(oldItemData, newItemData, out remainingCount);
        }

        public bool CanSwap(RunTimeItemData oldItemData, RunTimeItemData newItemData)
        {
            Debug.Log($"CanSwap: {oldItemData.AnchorPos} {newItemData.AnchorPos}");
            return gridPlacementService.CanSwap(oldItemData, newItemData);
        }

        /// <summary>
        /// 尝试移除物品
        /// </summary>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public InventoryOpResult TryRemoveItem(Vector2Int anchorPos)
        {
            var item = gridPlacementService.GetItemByMask(anchorPos);
            if (item is null)
            {
                return new InventoryOpResult(false, null);
            }
            if (!gridPlacementService.RemoveItemAny(anchorPos))
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
            return gridPlacementService.FindPlaceAtFirst(itemData, out anchorPos);
        }

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置 并放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public bool PlaceAtFirstItem(RunTimeItemData itemData, out Vector2Int anchorPos)
        {
            return gridPlacementService.PlaceAtFirst(itemData, out anchorPos);
        }

        /// <summary>
        /// 获取任意格上的物品
        /// </summary>
        /// <param name="anyPos"></param>
        /// <returns></returns>
        public RunTimeItemData GetItemAt(Vector2Int anyPos)
        {
            return gridPlacementService.GetItemByMask(anyPos);
        }

        /// <summary>
        /// 判断是否可以放置物品
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="anchorPos"></param>
        /// <returns></returns>
        public bool CanPlaceItem(RunTimeItemData itemData, Vector2Int anchorPos)
        {
            return gridPlacementService.CanPlace(itemData, anchorPos);
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
            // Debug.Log(
            //           "oldItemData.AnchorPos: " + oldItemData.AnchorPos +
            //           "/newItemData.AnchorPos: " + (newItemData == null ? "null" : newItemData.AnchorPos) +
            //           "/dragPreviewPos: " + dragPreviewAnchorPos);

            if (CanPlaceItem(oldItemData, dragPreviewAnchorPos))
            {
                return EFrameBoard.CanPlace;
            }
            else if (CanStack(oldItemData, newItemData, out int _))
            {
                return EFrameBoard.CanStack;
            }
            else if (gridPlacementService.TryGetSwapTargetItem(oldItemData, dragPreviewAnchorPos, out var swapTargetItem) &&
                     gridPlacementService.CanSwap(oldItemData, swapTargetItem, dragPreviewAnchorPos))
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
