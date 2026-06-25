using System;
using UnityEngine;

namespace MmInventory
{
    public partial class InventoryState
    {
        /// <summary>
        /// 放置服务类
        /// </summary>
        private sealed class InventoryPlacementService
        {
            /// <summary> 背包状态引用 </summary>
            private readonly InventoryState inventoryState;

            /// <summary>
            /// 初始化放置服务
            /// </summary>
            /// <param name="inventoryState">背包状态</param>
            public InventoryPlacementService(InventoryState inventoryState)
            {
                this.inventoryState = inventoryState;
            }

            #region 放置功能
            /// <summary>
            /// 检查物品是否可放置
            /// </summary>
            /// <param name="item">物品数据</param>
            /// <param name="anchorPos">锚点坐标</param>
            /// <returns>是否可放置</returns>
            public bool CanPlace(ItemRtData item, Vector2Int anchorPos)
            {
                // 检查锚点是否在背包范围内
                if (!inventoryState.IsInside(anchorPos))
                    return false;

                // 获取物品的占用尺寸
                var occupiedSize = item.DataSize;

                // 遍历占用尺寸
                for (int x = 0; x < occupiedSize.x; x++)
                {
                    for (int y = 0; y < occupiedSize.y; y++)
                    {
                        // 计算目标位置
                        Vector2Int targetPos = new Vector2Int(anchorPos.x + x, anchorPos.y + y);
                        // 检查目标位置是否在背包范围内
                        if (!inventoryState.IsInside(targetPos))
                            return false;

                        if (inventoryState.IsOccupied(targetPos))
                            return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// 按锚点设置物品
            /// </summary>
            /// <param name="anchorPos">锚点坐标</param>
            /// <param name="itemData">物品数据</param>
            /// <returns>是否成功</returns>
            public bool SetAt(Vector2Int anchorPos, ItemRtData itemData)
            {
                if (itemData is null || !CanPlace(itemData, anchorPos))
                    return false;

                inventoryState.SetItemData(itemData, anchorPos);
                return true;
            }

            /// <summary>
            /// 查找第一个可放置锚点
            /// </summary>
            /// <param name="itemData">物品数据</param>
            /// <param name="anchorPos">可放置锚点</param>
            /// <returns>是否找到</returns>
            public bool FindSetAtFirst(ItemRtData itemData, out Vector2Int anchorPos)
            {
                anchorPos = Vector2Int.zero;

                // 遍历所有背包格子 
                for (int y = 0; y < inventoryState.gridInventorySize.y; y++)
                {
                    for (int x = 0; x < inventoryState.gridInventorySize.x; x++)
                    {
                        // 计算候选锚点
                        var candidate = new Vector2Int(x, y);
                        // 检查是否可放置
                        if (CanPlace(itemData, candidate))
                        {
                            // 找到第一个可放置锚点 直接返回
                            anchorPos = candidate;
                            return true;
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// 查找并设置第一个可放置锚点
            /// </summary>
            /// <param name="itemData">物品数据</param>
            /// <param name="anchorPos">最终锚点</param>
            /// <returns>是否成功</returns>
            public bool SetAtFirst(ItemRtData itemData, out Vector2Int anchorPos)
            {
                if (!FindSetAtFirst(itemData, out anchorPos)) return false;
                return SetAt(anchorPos, itemData);
            }
            #endregion

            #region 删除功能
            /// <summary>
            /// 按锚点移除物品
            /// </summary>
            /// <param name="anchorPos">锚点坐标</param>
            /// <returns>是否成功</returns>
            public bool RemoveAt(Vector2Int anchorPos)
            {
                return inventoryState.RemoveItemData(anchorPos);
            }

            /// <summary>
            /// 按任意格子移除物品
            /// </summary>
            /// <param name="pos">任意格子坐标</param>
            /// <returns>是否成功</returns>
            public bool RemoveAtAny(Vector2Int pos)
            {
                var targetItem = GetItemByMask(pos);
                if (targetItem is null) return false;

                int anchorIndex = Array.FindIndex(
                    inventoryState.itemAnchorArray,
                    anchorItem => anchorItem != null && anchorItem.InstancedItemId == targetItem.InstancedItemId);

                if (anchorIndex == -1) return false;
                return inventoryState.RemoveItemData(inventoryState.ToVector2Int(anchorIndex));
            }
            #endregion

            #region 查询功能
            /// <summary>
            /// 获取锚点格子物品
            /// </summary>
            /// <param name="pos">格子坐标</param>
            /// <returns>物品数据</returns>
            public ItemRtData GetItemAt(Vector2Int pos)
            {
                // 检查位置是否在背包范围内
                return inventoryState.IsInside(pos) ? 
                       inventoryState.itemAnchorArray[inventoryState.ToIndex(pos)] : null;
            }

            /// <summary>
            /// 获取占用格子物品
            /// </summary>
            /// <param name="pos">格子坐标</param>
            /// <returns>物品数据</returns>
            public ItemRtData GetItemByMask(Vector2Int pos)
            {
                if (!inventoryState.IsInside(pos)) return null;
                return inventoryState.occupancyOwnerArray[inventoryState.ToIndex(pos)];
            }

            /// <summary>
            /// 判断两个区域是否相交
            /// </summary>
            /// <param name="aAnchorPos">区域A锚点</param>
            /// <param name="bAnchorPos">区域B锚点</param>
            /// <param name="aSize">区域A尺寸</param>
            /// <param name="bSize">区域B尺寸</param>
            /// <returns>是否相交</returns>
            public bool IsCover(Vector2Int aAnchorPos,
                                Vector2Int bAnchorPos,
                                Vector2Int aSize,
                                Vector2Int bSize)
            {
                bool noOverlap =
                    aAnchorPos.x + aSize.x <= bAnchorPos.x ||
                    bAnchorPos.x + bSize.x <= aAnchorPos.x ||
                    aAnchorPos.y + aSize.y <= bAnchorPos.y ||
                    bAnchorPos.y + bSize.y <= aAnchorPos.y;
                return !noOverlap;
            }
            #endregion
        }
    }
}
