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
        public ItemRtData aItemData;
        public ItemRtData bItemData;
        public ESwapState SwapState;
    }

    /// <summary>
    /// 这里是背包的核心算法与数据结构
    /// </summary>
    public partial class InventoryState
    {
        /// <summary>背包尺寸</summary>
        private readonly Vector2Int gridInventorySize;

        /// <summary>锚点数组 运行时记录每一个物品的锚点在哪里</summary>
        private ItemRtData[] itemAnchorArray;

        /// <summary>物品的全部占用格子信息</summary>
        private ItemRtData[] occupancyOwnerArray;

        /// <summary> 交换服务 </summary>
        private readonly InventorySwapService inventorySwapService;
        /// <summary> 放置服务 </summary>
        private readonly InventoryPlacementService inventoryPlacementService;


        /// <summary>
        /// 初始化背包状态
        /// </summary>
        /// <param name="gridInventorySize">背包尺寸</param>
        public InventoryState(Vector2Int gridInventorySize)
        {
            this.gridInventorySize = gridInventorySize;
            int totalCount = gridInventorySize.x * gridInventorySize.y;
            itemAnchorArray = new ItemRtData[totalCount];
            occupancyOwnerArray = new ItemRtData[totalCount];
            inventorySwapService = new InventorySwapService(this);
            inventoryPlacementService = new InventoryPlacementService(this);
        }

        /// <summary>
        /// 二维坐标 → 一维索引
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private int ToIndex(Vector2Int position) => 
            position.y * gridInventorySize.x + position.x;

        /// <summary>
        /// 一维索引 → 二维坐标
        /// </summary>
        /// <param name="index">一维索引</param>
        /// <returns>二维坐标</returns>
        private Vector2Int ToVector2Int(int index) => 
            new Vector2Int(index % gridInventorySize.x, index / gridInventorySize.x);
        
        /// <summary>
        /// 写入占用信息
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="anchorPos">锚点</param>
        /// <param name="occupied">是否占用</param>
        private void WriteOccupancy(ItemRtData item,
                                    Vector2Int anchorPos,
                                    bool occupied)
        {
            if (item is null) return;
            
            // 获取物品的占用尺寸
            var occupiedSize = item.DataSize;
            
            // 遍历占用尺寸
            for (int x = 0; x < occupiedSize.x; x++)
            {
                for (int y = 0; y < occupiedSize.y; y++)
                {
                    // 计算目标位置
                    var targetPos = new Vector2Int(anchorPos.x + x, anchorPos.y + y);
                    if (!IsInside(targetPos)) continue;

                    // 计算目标位置的索引
                    int index = ToIndex(targetPos);

                    // 如果需要占用 则写入数组 如果不需要则情况对应位置
                    if (occupied)
                        occupancyOwnerArray[index] = item;
                    else
                        occupancyOwnerArray[index] = null;
                }
            }
        }

        /// <summary>
        /// 设置锚点物品并更新占用信息
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="anchorPos">锚点</param>
        private void SetItemData(ItemRtData item, Vector2Int anchorPos)
        {
            itemAnchorArray[ToIndex(anchorPos)] = item;
            WriteOccupancy(item, anchorPos, true);
        }

        /// <summary>
        /// 移除锚点物品并更新占用信息
        /// </summary>
        /// <param name="anchorPos">锚点</param>
        /// <returns>是否成功</returns>
        private bool RemoveItemData(Vector2Int anchorPos)
        {
            if (!IsInside(anchorPos)) return false;

            // 获取目标位置的索引
            int index = ToIndex(anchorPos);
            // 获取目标位置的物品
            var item = itemAnchorArray[index];
            // 如果物品为空 则返回失败
            if (item is null) return false;

            // 清空目标位置的物品
            itemAnchorArray[index] = null;
            // 更新占用信息
            WriteOccupancy(item, anchorPos, false);
            return true;
        }

        /// <summary>
        /// 范围判定
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否在背包范围内</returns>
        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridInventorySize.x &&
                   position.y >= 0 && position.y < gridInventorySize.y;
        }

        /// <summary>
        /// 判断目标格子是否已被占用
        /// </summary>
        /// <param name="pos">格子坐标</param>
        /// <returns>是否被占用</returns>
        private bool IsOccupied(Vector2Int pos) =>
            occupancyOwnerArray[ToIndex(pos)] != null;

        #region 放置功能
        /// <summary>
        /// 放置判定
        /// </summary>
        public bool CanPlace(ItemRtData item, Vector2Int anchorPos) =>
        inventoryPlacementService.CanPlace(item, anchorPos);

        /// <summary>
        /// 放置物品
        /// </summary>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <param name="itemData"> 物品数据 </param>
        public bool SetAt(Vector2Int anchorPos, ItemRtData itemData) =>
        inventoryPlacementService.SetAt(anchorPos, itemData);

        /// <summary>
        /// 遍历所有格子 放置物品到第一个可放置位置
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns></returns>
        public bool FindSetAtFirst(ItemRtData itemData, out Vector2Int anchorPos) =>
        inventoryPlacementService.FindSetAtFirst(itemData, out anchorPos);

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置 并放置物品
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns> 是否成功 </returns>
        public bool SetAtFirst(ItemRtData itemData, out Vector2Int anchorPos) =>
        inventoryPlacementService.SetAtFirst(itemData, out anchorPos);
        #endregion

        #region 堆叠功能
        /// <summary>
        /// 堆叠判定
        /// </summary>
        /// <param name="aItemData">拖动物品</param>
        /// <param name="bItemData">目标物品</param>
        /// <returns></returns>
        public bool CanStack(ItemRtData aItemData, ItemRtData bItemData, out int remainingCount)
        {
            remainingCount = 0;

            if (aItemData is null || bItemData is null) return false;
            // 物品是否相同
            if (aItemData.PersistenceItemId != bItemData.PersistenceItemId)
                return false;

            // 堆叠类型是否为可堆
            var a = ItemRtDataMgr.Instance.GetItemData<IItemRootData>(aItemData.PersistenceItemId);
            var b = ItemRtDataMgr.Instance.GetItemData<IItemRootData>(bItemData.PersistenceItemId);
            if (a.ItemStackType is EItemStackType.Single || b.ItemStackType is EItemStackType.Single)
                return false;

            // 计算剩余数量 向b堆叠 所以是b + a - b.MaxStackCount
            remainingCount = b.MaxStackCount - (aItemData.CurStackCount + bItemData.CurStackCount);
            return true;
        }
        #endregion

        #region 交换功能

        public bool CanSwap(ItemRtData aItemData,
                            ItemRtData bItemData,
                            Vector2Int placeAnchorPos) =>
        inventorySwapService.CanSwap(aItemData, bItemData, placeAnchorPos);


        public bool TrySwap(ItemRtData aItemData,
                            ItemRtData bItemData,
                            List<ItemRtData> oldItemDataList,
                            Vector2Int placeAnchorPos) =>
        inventorySwapService.TrySwap(aItemData, bItemData, oldItemDataList, placeAnchorPos);
        
        /// <summary>
        /// 尝试获取交换目标物品信息
        /// </summary>
        /// <param name="dragItemData">拖动物品</param>
        /// <param name="placeAnchorPos">放置锚点</param>
        /// <param name="swapTargetItem">交换目标物品</param>
        /// <returns></returns>
        public bool TryGetSwapTargetItem(ItemRtData dragItemData,
                                         Vector2Int placeAnchorPos,
                                         out ItemRtData swapTargetItem) =>
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
        public ItemRtData GetItemAt(Vector2Int pos) =>
        inventoryPlacementService.GetItemAt(pos);
        /// <summary>
        /// 获取格子上的物品
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public ItemRtData GetItemByMask(Vector2Int pos) =>
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