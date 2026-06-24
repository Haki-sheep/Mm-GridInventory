using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 交换状态
    /// </summary>
    public enum ESwapState
    {
        // 不能交换
        CanNotSwap,
        // 相同物品
        Same,
        // 大物品到小物品
        LargeToSmall,
        // 小物品到大物品
        SmallToLarge,
    }

    /// <summary>
    /// 交换结构体 
    /// aItemData: 交换的物品
    /// bItemData: 交换的物品
    /// SwapState: 交换状态
    /// </summary>
    public struct SwapPlan
    {
        public RunTimeItemData aItemData;
        public RunTimeItemData bItemData;
        public ESwapState SwapState;
    }

    /// <summary>
    /// 背包站位数据
    /// </summary>
    public partial class InventoryState
    {
        // X=宽度(列数) Y=高度(行数)
        private Vector2Int gridInventorySize;

        // 锚点数组
        private RunTimeItemData[] runTimeItemDataArray;

        // 占用掩码
        private bool[] mask;

        // 占用者数组 用于记录每个格子的占用者
        private RunTimeItemData[] occupancyOwnerArray;

        /// <summary> 交换服务 </summary>
        private readonly InventorySwapService inventorySwapService;
        /// <summary> 放置服务 </summary>
        private readonly InventoryPlacementService inventoryPlacementService;


        public InventoryState(Vector2Int gridInventorySize)
        {
            this.gridInventorySize = gridInventorySize;
            int totalCount = gridInventorySize.x * gridInventorySize.y;
            runTimeItemDataArray = new RunTimeItemData[totalCount];
            occupancyOwnerArray = new RunTimeItemData[totalCount];
            mask = new bool[totalCount];
            inventorySwapService = new InventorySwapService(this);
            inventoryPlacementService = new InventoryPlacementService(this);
        }

        // 二维坐标 → 一维索引
        private int ToIndex(Vector2Int position) => 
        position.y * gridInventorySize.x + position.x;

        // 一维索引 → 二维坐标
        private Vector2Int ToPosition(int index) => 
        new Vector2Int(index % gridInventorySize.x, index / gridInventorySize.x);

        // 当前占用尺寸
        private static Vector2Int GetOccupiedSize(RunTimeItemData item) => item.DataSize;

        /// <summary>
        /// 更新占用信息
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="anchorPos">锚点</param>
        /// <param name="occupied">是否占用</param>
        private void WriteOccupancy(RunTimeItemData item, Vector2Int anchorPos, bool occupied)
        {
            if (item is null) return;

            var occupiedSize = GetOccupiedSize(item);
            for (int x = 0; x < occupiedSize.x; x++)
            {
                for (int y = 0; y < occupiedSize.y; y++)
                {
                    var targetPos = new Vector2Int(anchorPos.x + x, anchorPos.y + y);
                    if (!IsInside(targetPos)) continue;

                    int idx = ToIndex(targetPos);
                    mask[idx] = occupied;
                    occupancyOwnerArray[idx] = occupied ? item : null;
                }
            }
        }

        /// <summary>
        /// 设置锚点物品并更新占用信息
        /// </summary>
        /// <param name="anchorPos">锚点</param>
        /// <param name="item">物品</param>
        private void SetAnchorItem(Vector2Int anchorPos, RunTimeItemData item)
        {
            runTimeItemDataArray[ToIndex(anchorPos)] = item;
            WriteOccupancy(item, anchorPos, true);
        }

        /// <summary>
        /// 移除锚点物品并更新占用信息
        /// </summary>
        /// <param name="anchorPos">锚点</param>
        /// <returns>是否成功</returns>
        private bool RemoveAnchorItem(Vector2Int anchorPos)
        {
            if (!IsInside(anchorPos)) return false;

            int index = ToIndex(anchorPos);
            var item = runTimeItemDataArray[index];
            if (item is null) return false;

            runTimeItemDataArray[index] = null;
            WriteOccupancy(item, anchorPos, false);
            return true;
        }

        #region 边界判定
        /// <summary>
        /// 范围判定
        /// </summary>
        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridInventorySize.x &&
                   position.y >= 0 && position.y < gridInventorySize.y;
        }
        #endregion

        #region 放置功能（增/改）
        /// <summary>
        /// 放置判定
        /// </summary>
        public bool CanPlace(RunTimeItemData item, Vector2Int anchorPos) =>
        inventoryPlacementService.CanPlace(item, anchorPos);

        /// <summary>
        /// 放置物品
        /// </summary>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <param name="itemData"> 物品数据 </param>
        public bool SetAt(Vector2Int anchorPos, RunTimeItemData itemData) =>
        inventoryPlacementService.SetAt(anchorPos, itemData);

        /// <summary>
        /// 遍历所有格子 放置物品到第一个可放置位置
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns></returns>
        public bool FindSetAtFirst(RunTimeItemData itemData, out Vector2Int anchorPos) =>
        inventoryPlacementService.FindSetAtFirst(itemData, out anchorPos);

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置 并放置物品
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns> 是否成功 </returns>
        public bool SetAtFirst(RunTimeItemData itemData, out Vector2Int anchorPos) =>
        inventoryPlacementService.SetAtFirst(itemData, out anchorPos);
        #endregion

        #region 堆叠功能
        /// <summary>
        /// 堆叠判定
        /// </summary>
        /// <param name="aItemData">拖动物品</param>
        /// <param name="bItemData">目标物品</param>
        /// <returns></returns>
        public bool CanStack(RunTimeItemData aItemData, RunTimeItemData bItemData, out int remainingCount)
        {
            remainingCount = 0;

            if (aItemData is null || bItemData is null) return false;
            // 物品是否相同
            if (aItemData.PersistenceItemId != bItemData.PersistenceItemId)
                return false;

            // 堆叠类型是否为可堆
            var a = RunTimeItemDataMgr.Instance.GetItemData<IItemRootData>(aItemData.PersistenceItemId);
            var b = RunTimeItemDataMgr.Instance.GetItemData<IItemRootData>(bItemData.PersistenceItemId);
            if (a.ItemStackType is EItemStackType.Single || b.ItemStackType is EItemStackType.Single)
                return false;

            // 计算剩余数量 向b堆叠 所以是b + a - b.MaxStackCount
            remainingCount = b.MaxStackCount - (aItemData.CurStackCount + bItemData.CurStackCount);
            return true;
        }
        #endregion

        #region 交换功能


        public bool CanSwap(RunTimeItemData aItemData,
                            RunTimeItemData bItemData,
                            Vector2Int placeAnchorPos) =>
        inventorySwapService.CanSwap(aItemData, bItemData, placeAnchorPos);


        public bool TrySwap(RunTimeItemData aItemData,
                            RunTimeItemData bItemData,
                            out List<RunTimeItemData> oldItemDataList,
                            Vector2Int placeAnchorPos) =>
        inventorySwapService.TrySwap(aItemData, bItemData, out oldItemDataList, placeAnchorPos);
        
        /// <summary>
        /// 尝试获取交换目标物品信息
        /// </summary>
        /// <param name="dragItemData">拖动物品</param>
        /// <param name="placeAnchorPos">放置锚点</param>
        /// <param name="swapTargetItem">交换目标物品</param>
        /// <returns></returns>
        public bool TryGetSwapTargetItem(RunTimeItemData dragItemData,
                                         Vector2Int placeAnchorPos,
                                         out RunTimeItemData swapTargetItem) =>
        inventorySwapService.TryGetSwapTargetItem(dragItemData, placeAnchorPos, out swapTargetItem);


        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveAt(Vector2Int anchorPos) =>
        inventoryPlacementService.RemoveAt(anchorPos);

        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveAtAny(Vector2Int pos) =>
        inventoryPlacementService.RemoveAtAny(pos);
        #endregion

        #region 查询功能
        /// <summary>
        /// 获取格子物品
        /// </summary>
        public RunTimeItemData GetItemAt(Vector2Int pos) =>
        inventoryPlacementService.GetItemAt(pos);
        /// <summary>
        /// 获取格子上的物品
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public RunTimeItemData GetItemByMask(Vector2Int pos) =>
        inventoryPlacementService.GetItemByMask(pos);

        /// <summary>
        /// 判断a是否覆盖b
        /// </summary>
        /// <param name="aAnchorPos">a的锚点</param>
        /// <param name="bAnchorPos">b的锚点</param>
        /// <param name="aSize">a的尺寸</param>
        /// <param name="bSize">b的尺寸</param>
        /// <returns></returns>
        public bool IsCover(Vector2Int aAnchorPos, Vector2Int bAnchorPos, Vector2Int aSize, Vector2Int bSize) =>
        inventoryPlacementService.IsCover(aAnchorPos, bAnchorPos, aSize, bSize);
        #endregion
    }
}